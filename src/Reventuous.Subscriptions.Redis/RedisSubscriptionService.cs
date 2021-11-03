using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Reventuous.Subscriptions.Redis {
    [PublicAPI]
    public abstract class RedisSubscriptionService : SubscriptionService {
        protected IDatabase Database { get; }

        protected RedisSubscriptionService(
            IDatabase              database,
            RedisSubscriptionOptions options,
            ICheckpointStore              checkpointStore,
            IEnumerable<IEventHandler>    eventHandlers,
            IEventSerializer?             eventSerializer = null,
            ILoggerFactory?               loggerFactory   = null,
            ISubscriptionGapMeasure?      measure         = null
        ) : base(options, checkpointStore, eventHandlers, eventSerializer, loggerFactory, measure) {
            Database = Ensure.NotNull(database, nameof(database));
        }
    }
}