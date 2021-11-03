using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Reventuous.Subscriptions.Redis
{
    public class RedisSubscription : IDisposable
    {
        IDatabase _database;
        string _stream;
        string _checkpoint;
        int _readDelay;
        int _numberOfMessages;
        Func<StreamEntry, CancellationToken, Task> _onReceived;
        private readonly CancellationTokenSource _disposed;

        public async static Task<RedisSubscription> Confirm(
            IDatabase           database,
            string              stream,
            string              checkpoint,
            int                 readDelay,
            int                 numberOfMessages,
            Func<StreamEntry, CancellationToken, Task> onReceived,
            Action<SubscriptionDroppedReason> onSubscriptionDropped,
            CancellationToken   cancellationToken = default
        )
        {
            await database.StreamInfoAsync(stream);
            return new RedisSubscription(
                database,
                stream,
                checkpoint,
                readDelay,
                numberOfMessages,
                onReceived,
                onSubscriptionDropped,
                cancellationToken
            );
        }

        public RedisSubscription(
            IDatabase           database,
            string              stream,
            string              checkpoint,
            int                 readDelay,
            int                 numberOfMessages,
            Func<StreamEntry, CancellationToken, Task> onReceived,
            Action<SubscriptionDroppedReason> onSubscriptionDropped,
            CancellationToken   cancellationToken = default

        ) {
            _database = database;
            _stream = stream;
            _readDelay = readDelay;
            _numberOfMessages = numberOfMessages;
            _onReceived = onReceived;
            _checkpoint = checkpoint;
            _disposed = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Run(Subscribe);
        }

        public void Dispose()
        {
			if (_disposed.IsCancellationRequested) {
				return;
			}
			_disposed.Cancel();
			_disposed.Dispose();
        }

        private async Task Subscribe()
        {
            var position = _checkpoint;
            while(!_disposed.IsCancellationRequested)
            {
                var entries = await _database.StreamReadAsync(
                    _stream, 
                    position, 
                    count:_numberOfMessages);
                
                if (entries.Length == 0)
                {
                    Thread.Sleep(_readDelay);
                }
                else
                {
                    foreach(var entry in entries)
                    {
                        var e = entry;
                        position = e.Id;
                        if (e["stream"] != default(RedisValue)) // if this is a linked event
                        {
                            var streamEntries = await _database.StreamRangeAsync((string) e["stream"], e["position"], null, 1);
                            e = streamEntries.FirstOrDefault();
                        }

                        await _onReceived(e, _disposed.Token);
                    }
                }
            }
        }


    }

}