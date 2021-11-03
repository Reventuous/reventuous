using System;
using System.Threading;
using System.Threading.Tasks;
using Reventuous;
using Reventuous.Subscriptions;
using Reventuous.Subscriptions.Redis;
using StackExchange.Redis;

namespace Subscriptions.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            EventMapping.MapEventTypes();
            //MySubscription.PersistentSubscribe().Wait();
            //MySubscription.Subscribe().Wait();
            MySubscription.SubscribeAll().Wait();
            Thread.Sleep(1000 * 60);
        }
    }

    public static class MySubscription
    {
        public static async Task Subscribe()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();

            var streamName = "account1";
            var subscriptionId = "mysubscription";

            var sub = new StreamSubscription(
                db,
                streamName,
                subscriptionId,
                new NoOpCheckpointStore(),
                new IEventHandler[] {
                    new MyHandler(
                        subscriptionId
                    )
                },
                DefaultEventSerializer.Instance
            );

            await sub.StartAsync(default);
        }

        public static async Task SubscribeAll()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();

            var subscriptionId = "mysubscription";

            var sub = new AllStreamSubscription(
                db,
                subscriptionId,
                new NoOpCheckpointStore(),
                new IEventHandler[] {
                    new MyHandler(
                        subscriptionId
                    )
                },
                DefaultEventSerializer.Instance
            );

            await sub.StartAsync(default);
        }

        public static async Task PersistentSubscribe()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();

            var subscriptionId = "mypermanentsubscription2";
            var streamName = "account1";
            var consumerId = "myconsumer";

            var sub = new StreamPersistentSubscription(
                db,
                streamName,
                subscriptionId,
                consumerId,
                new NoOpCheckpointStore(),
                new IEventHandler[] {
                    new MyHandler(
                        subscriptionId
                    )
                },
                DefaultEventSerializer.Instance
            );

            await sub.StartAsync(default);
        }

    }

    public class MyHandler : IEventHandler
    {
        public string SubscriptionId { get; }
        public MyHandler(
            string subscriptionId
        )
        {
            SubscriptionId = subscriptionId;
        }

        public async Task HandleEvent(
            object @event, 
            string position,
            CancellationToken cancellationToken
        )
        {
            System.Console.WriteLine(@event.ToString());
        }
    }


}
