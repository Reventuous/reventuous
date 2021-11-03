using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Reventuous.Subscriptions.Redis
{
    public class RedisPersistentSubscription : IDisposable
    {
        IDatabase _database;
        string  _group;
        string  _consumer;
        string _stream;
        string _checkpoint;
        int _readDelay;
        Func<StreamEntry, CancellationToken, Task> _onReceived;
        Action<SubscriptionDroppedReason> _onSubscriptionDropped;
        private readonly CancellationTokenSource _disposed;

        public static async Task<RedisPersistentSubscription> Confirm(
            IDatabase           database,
            string              group,
            string              consumer,
            string              stream,
            string              checkpoint,
            int                readDelay,
            Func<StreamEntry, CancellationToken, Task> onReceived,
            Action<SubscriptionDroppedReason> onSubscriptionDropped,
            CancellationToken   cancellationToken = default
        )
        {
            await database.StreamInfoAsync(stream);
            try 
            {
                await database.StreamCreateConsumerGroupAsync(
                    stream, 
                    group, 
                    checkpoint);
            }
            catch(RedisServerException e)
            {
                // ignore if the subscription group exists already
                if (!e.Message.Contains("BUSYGROUP"))
                    throw;
            }
            return new RedisPersistentSubscription(
                database,
                group,
                consumer,
                stream,
                checkpoint,
                readDelay,
                onReceived,
                onSubscriptionDropped,
                cancellationToken
            );
        }

        public RedisPersistentSubscription(
            IDatabase           database,
            string              group,
            string              consumer,
            string              stream,
            string              checkpoint,
            int                readDelay,
            Func<StreamEntry, CancellationToken, Task> onReceived,
            Action<SubscriptionDroppedReason> onSubscriptionDropped,
            CancellationToken   cancellationToken = default

        ) {
            _database = database;
            _group = group;
            _consumer = consumer;
            _stream = stream;
            _readDelay = readDelay;
            _onReceived = onReceived;
            _onSubscriptionDropped = onSubscriptionDropped;
            _checkpoint = checkpoint;
            _disposed = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Run(Subscribe);
        }

        public void Dispose()
        {
			if (_disposed.IsCancellationRequested) {
                _onSubscriptionDropped(SubscriptionDroppedReason.Disposed);
				return;
			}
			_disposed.Cancel();
			_disposed.Dispose();
        }

        private async Task Subscribe()
        {
            while(!_disposed.IsCancellationRequested)
            {
                var entries = await _database.StreamReadGroupAsync(
                    _stream, 
                    _group, 
                    _consumer); 
                
                if (entries.Length == 0) 
                {
                    Thread.Sleep(_readDelay);
                }
                else
                {
                    foreach(var entry in entries)
                    {
                        var e = entry;
                        if (e["stream"] != default(RedisValue)) // if this is a linked event
                        {
                            var streamEntries = await _database.StreamRangeAsync((string) e["stream"], e["position"], null, 1);
                            e = streamEntries.FirstOrDefault();
                        }

                        await _onReceived(e, _disposed.Token);
                        await _database.StreamAcknowledgeAsync(_stream, _group, entry.Id);
                    }
                }
            }
        }


    }

}