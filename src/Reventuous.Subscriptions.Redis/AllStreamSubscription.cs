using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Reventuous.Subscriptions.Redis {
    /// <summary>
    /// Catch-up subscription for Redis, using the $all stream
    /// Important: Redis Streams doesn't come with an $all stream like EventStoreDB.
    /// However, this stream can be created automatically by following these steps:
    /// 1. Add the Redis Gears module to the Redis instance that is used as event store
    /// 2. Register the Redis Gears function defined in projections.py
    /// </summary>
    [PublicAPI]
    public class AllStreamSubscription : RedisSubscriptionService {
        readonly AllStreamSubscriptionOptions _options;

        /// <summary>
        /// Creates Redis streams catch-up subscription service for $all
        /// </summary>
        /// <param name="database">Redis database that will act as event store for events</param>
        /// <param name="subscriptionId">Subscription ID (aka Consumer Group)</param>
        /// <param name="consumerId">Multiple consumers can subscribe for the same subscription id (e.g. consumer group)</param>
        /// <param name="checkpointStore">Checkpoint store instance</param>
        /// <param name="eventSerializer">Event serializer instance</param>
        /// <param name="eventHandlers">Collection of event handlers</param>
        /// <param name="loggerFactory">Optional: logger factory</param>
        /// <param name="eventFilter">Optional: server-side event filter</param>
        /// <param name="measure">Optional: gap measurement for metrics</param>
        public AllStreamSubscription(
            IDatabase                  database,
            string                     subscriptionId,
            ICheckpointStore           checkpointStore,
            IEnumerable<IEventHandler> eventHandlers,
            IEventSerializer?          eventSerializer = null,
            ILoggerFactory?            loggerFactory   = null
        ) : this(
            database,
            new AllStreamSubscriptionOptions {
                SubscriptionId = subscriptionId,
            },
            checkpointStore,
            eventHandlers,
            eventSerializer,
            loggerFactory
        ) { }

        /// <summary>
        /// Creates Redis catch-up subscription service for $all
        /// </summary>
        /// <param name="database">the Redis database that will act as event store for events</param>
        /// <param name="options"></param>
        /// <param name="checkpointStore">Checkpoint store instance</param>
        /// <param name="eventSerializer">Event serializer instance</param>
        /// <param name="eventHandlers">Collection of event handlers</param>
        /// <param name="loggerFactory">Optional: logger factory</param>
        /// <param name="measure">Optional: gap measurement for metrics</param>
        public AllStreamSubscription(
            IDatabase                    database,
            AllStreamSubscriptionOptions options,
            ICheckpointStore             checkpointStore,
            IEnumerable<IEventHandler>   eventHandlers,
            IEventSerializer?            eventSerializer = null,
            ILoggerFactory?              loggerFactory   = null
        ) : base(
            database,
            options,
            checkpointStore,
            eventHandlers,
            eventSerializer,
            loggerFactory
        ) {
            _options     = options;
        }

        protected override async Task<EventSubscription> Subscribe(
            Checkpoint        checkpoint,
            CancellationToken cancellationToken
        ) {

            var subTask = RedisSubscription.Confirm(
                Database,
                _options.StreamName,
                checkpoint.Position,
                _options.ReadDelay,
                _options.NumberOfMessages,
                HandleEntry,
                HandleDrop,
                cancellationToken
            );

            await subTask.NoContext();

            return new EventSubscription(
                SubscriptionId, new Stoppable(() => subTask.Dispose()));

            async Task HandleEntry(StreamEntry entry, CancellationToken ct)
            {
                await Handler(AsReceivedEntry(entry), ct).NoContext();
            }
            void HandleDrop(SubscriptionDroppedReason reason)
                => Dropped(RedisMappings.AsDropReason(reason), null);

            ReceivedEvent AsReceivedEntry(StreamEntry entry) 
            {
                var @event = DeserializeData(
                    "application/json",
                    entry["type"],
                    entry["data"],
                    _options.StreamName,
                    entry.Id
                );
                
                return new ReceivedEvent(
                    entry.Id,
                    entry["type"],
                    "application/json",
                    _options.StreamName,
                    entry.Id,
                    entry.Id.ToString().ToDateAndTime(),
                    @event
                    // re.Event.Metadata
                );
            }
        }
    }
}