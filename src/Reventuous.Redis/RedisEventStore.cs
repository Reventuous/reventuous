using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using StackExchange.Redis;
using JetBrains.Annotations;

namespace Reventuous.Redis
{
    [PublicAPI]
    public class RedisEventStore : IEventStore
    {
        readonly IDatabase _database;

        public RedisEventStore(IDatabase database) => _database = database;

        public async Task<AppendEventsResult> AppendEvents(
            string                           stream,
            ExpectedStreamVersion            expectedVersion,
            IReadOnlyCollection<StreamEvent> events,
            CancellationToken                cancellationToken
        ) 
        {
            var tran = _database.CreateTransaction();
            Task<RedisValue> lastEventIdTask = Task.FromResult<RedisValue>(default);

            if (expectedVersion != ExpectedStreamVersion.NoStream  && expectedVersion != ExpectedStreamVersion.Any)
            {
                tran.AddCondition(Condition.StreamLengthEqual(stream, expectedVersion.Value + 1));
            }
            foreach(StreamEvent streamEvent in events)
            {
                lastEventIdTask = tran.StreamAddAsync(stream, ToEventData(streamEvent));
            }

            if (tran.Execute() == false)
            {
                if (expectedVersion.Value > 0)
                    throw new Exceptions.WrongExpectedVersionException(stream);
            }

            var lastEventId = await lastEventIdTask;
            return new AppendEventsResult(
                lastEventId,
                expectedVersion.Value + events.Count + 1
            );

            static NameValueEntry[] ToEventData(StreamEvent streamEvent)
                => new NameValueEntry[]
                {
                    new NameValueEntry("type", streamEvent.EventType),
                    new NameValueEntry("data", streamEvent.Data)
                };
        }

        public async Task<StreamEvent[]> ReadEvents(
            string stream, 
            StreamReadPosition start, 
            int count, 
            CancellationToken cancellationToken) 
        {
            var position = new RedisValue(start);
            StreamEntry[] entries = await _database.StreamRangeAsync(stream, position, null, count);
            if (entries == null)
                throw new Exceptions.StreamNotFound(stream);
            return ToStreamEvents(entries);        
        }

        public async Task<StreamEvent[]> ReadEventsBackwards(
            string stream, 
            int count, 
            CancellationToken cancellationToken) 
        {
            StreamEntry[] entries = await _database.StreamRangeAsync(stream, null, null, count, Order.Descending);
            if (entries == null)
                throw new Exceptions.StreamNotFound(stream);
            return ToStreamEvents(entries);        
        }

        public async Task ReadStream(
            string              stream,
            StreamReadPosition  start,
            Action<StreamEvent> callback,
            CancellationToken   cancellationToken
        ) {
            var position = new RedisValue(start);
            StreamEntry[] entries = await _database.StreamReadAsync(stream, position, null);
            if (entries == null)
                throw new Exceptions.StreamNotFound(stream);
            foreach (var entry in entries) {
                callback(ToStreamEvent(entry));
            }
        }

        static StreamEvent ToStreamEvent(StreamEntry resolvedEvent)
            => new(
                resolvedEvent["type"],
                resolvedEvent["data"],
                null,
                "application/json"
            );

        static StreamEvent[] ToStreamEvents(StreamEntry[] resolvedEvents)
            => resolvedEvents.Select(ToStreamEvent).ToArray();

        static string StreamName(string stream)
            => $"stream:{stream}";

    }
}
