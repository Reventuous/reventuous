using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reventuous.Subscriptions {
    [PublicAPI]
    public abstract class SubscriptionService : IHostedService, IReportHealth {
        internal  bool              IsRunning      { get; set; }
        internal  bool              IsDropped      { get; set; }
        public    string            SubscriptionId { get; }
        protected EventSubscription Subscription   { get; set; } = null!;
        protected Logging?          DebugLog       { get; }
        protected ILogger?          Log            { get; }

        readonly ICheckpointStore         _checkpointStore;
        readonly IEventSerializer         _eventSerializer;
        readonly IEventHandler[]          _eventHandlers;
        readonly ISubscriptionGapMeasure? _measure;
        readonly bool                     _throwOnError;
        EventPosition?           _lastProcessed;

        protected SubscriptionService(
            SubscriptionOptions        options,
            ICheckpointStore           checkpointStore,
            IEnumerable<IEventHandler> eventHandlers,
            IEventSerializer?          eventSerializer = null,
            ILoggerFactory?            loggerFactory   = null,
            ISubscriptionGapMeasure?   measure         = null
        ) {
            _checkpointStore = Ensure.NotNull(checkpointStore, nameof(checkpointStore));
            _eventSerializer = eventSerializer ?? DefaultEventSerializer.Instance;
            SubscriptionId   = Ensure.NotEmptyString(options.SubscriptionId, options.SubscriptionId);
            _measure         = measure;
            _throwOnError    = options.ThrowOnError;

            _eventHandlers = Ensure.NotNull(eventHandlers, nameof(eventHandlers))
                .Where(x => x.SubscriptionId == options.SubscriptionId)
                .ToArray();

            Log = loggerFactory?.CreateLogger($"StreamSubscription-{options.SubscriptionId}");

            DebugLog = Log?.IsEnabled(LogLevel.Debug) == true ? Log.LogDebug : null;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {

            var checkpoint = await _checkpointStore.GetLastCheckpoint(SubscriptionId, cancellationToken).NoContext();

            _lastProcessed = new EventPosition(checkpoint.Position, DateTime.Now);

            Subscription = await Subscribe(checkpoint, cancellationToken).NoContext();

            IsRunning = true;

            Log?.LogInformation("Started subscription {Subscription}", SubscriptionId);
        }

        protected async Task Handler(ReceivedEvent re, CancellationToken cancellationToken) {
            DebugLog?.Invoke(
                "Subscription {Subscription} got an event {EventType}",
                SubscriptionId,
                re.EventType
            );

            _lastProcessed = GetPosition(re);

            try {
                if (re.Payload != null) {
                    await Task.WhenAll(
                        _eventHandlers.Select(x => x.HandleEvent(re.Payload, re.EventId, cancellationToken))
                    ).NoContext();
                }
            }
            catch (Exception e) {
                Log?.Log(
                    _throwOnError ? LogLevel.Error : LogLevel.Warning,
                    e,
                    "Error when handling the event {Stream} {Position} {Type}",
                    re.Stream,
                    re.EventId,
                    re.EventType
                );

                if (_throwOnError)
                    throw new SubscriptionException(re.Stream, re.EventType, re.EventId, re.Payload, e);
            }

            await Store().NoContext();

            Task Store() => StoreCheckpoint(GetPosition(re), cancellationToken);

            static EventPosition GetPosition(ReceivedEvent receivedEvent)
                => new(receivedEvent.Position, receivedEvent.Created);
        }

        protected async Task StoreCheckpoint(EventPosition position, CancellationToken cancellationToken) {
            _lastProcessed = position;
            var checkpoint = new Checkpoint(SubscriptionId, position.Position);

            await _checkpointStore.StoreCheckpoint(checkpoint, cancellationToken).NoContext();
        }

        protected object? DeserializeData(
            string eventContentType, 
            string eventType, 
            ReadOnlyMemory<byte> data, 
            string stream, 
            string position) 
        {
            if (data.IsEmpty) return null;
            
            var contentType = string.IsNullOrWhiteSpace(eventType) ? "application/json" : eventContentType;

            if (contentType != _eventSerializer.ContentType) {
                Log?.LogError(
                    "Unknown content type {ContentType} for event {Stream} {Position} {Type}",
                    contentType,
                    stream,
                    position,
                    eventType
                );

                if (_throwOnError)
                    throw new InvalidOperationException($"Unknown content type {contentType}");
            }

            try {
                return _eventSerializer.Deserialize(data.Span, eventType);
            }
            catch (Exception e) {
                Log?.LogError(
                    e,
                    "Error deserializing event {Strean} {Position} {Type}",
                    stream,
                    position,
                    eventType
                );

                if (_throwOnError)
                    throw new DeserializationException(stream, eventType, position, e);

                return null;
            }
            
        }

        protected abstract Task<EventSubscription> Subscribe(
            Checkpoint        checkpoint,
            CancellationToken cancellationToken
        );

        public async Task StopAsync(CancellationToken cancellationToken) {
            IsRunning = false;

            await Subscription.Stop(cancellationToken).NoContext();

            Log?.LogInformation("Stopped subscription {Subscription}", SubscriptionId);
        }

        readonly InterlockedSemaphore _resubscribing = new();

        protected async Task Resubscribe(TimeSpan delay) {
            if (_resubscribing.IsClosed()) return;
            
            Log?.LogWarning("Resubscribing {Subscription}", SubscriptionId);

            await Task.Delay(delay).NoContext();

            while (IsRunning && IsDropped) {
                try {
                    _resubscribing.Close();

                    var checkpoint = new Checkpoint(SubscriptionId, _lastProcessed?.Position!);

                    Subscription = await Subscribe(checkpoint, default).NoContext();

                    IsDropped = false;

                    Log?.LogInformation("Subscription {Subscription} restored", SubscriptionId);
                }
                catch (Exception e) {
                    Log?.LogError(e, "Unable to restart the subscription {Subscription}", SubscriptionId);

                    await Task.Delay(1000).NoContext();
                }
                finally {
                    _resubscribing.Open();
                }
            }
        }

        protected void Dropped(
            DropReason reason,
            Exception? exception
        ) {
            if (!IsRunning || _resubscribing.IsClosed()) return;

            Log?.LogWarning(
                exception,
                "Subscription {Subscription} dropped {Reason}",
                SubscriptionId,
                reason
            );

            IsDropped      = true;
            _lastException = exception;

            Task.Run(
                () => Resubscribe(
                    reason == DropReason.Stopped ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(2)
                )
            );
        }

        Exception? _lastException;

        public SubscriptionHealth Health => new(!(IsRunning && IsDropped), _lastException);
    }

    public record EventPosition(string Position, DateTime Created);
}