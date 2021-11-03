using System.Text.Json;
using StackExchange.Redis;
using Reventuous;
using Reventuous.Redis;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Reventuous.Tests.Fixtures
{
    public class Fixture
    {
        public IDatabase Database { get; }
        public IAggregateStore AggregateStore { get; }
        public IEventStore EventStore { get; }

        public IEventSerializer Serializer { get; } = new DefaultEventSerializer(
            new JsonSerializerOptions(JsonSerializerDefaults.Web).ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)
        );

        public Fixture()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            Database = redis.GetDatabase();
            EventStore = new RedisEventStore(Database);
            AggregateStore = new AggregateStore(EventStore, Serializer);
        }

    }
}