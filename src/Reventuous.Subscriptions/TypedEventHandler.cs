using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Reventuous.Subscriptions {
    [PublicAPI]
    public abstract class TypedEventHandler : IEventHandler {
        public abstract string SubscriptionId { get; }

        readonly Dictionary<Type, HandleUntypedEvent> _handlersMap = new();

        protected void On<T>(HandleTypedEvent<T> handler) where T : class {
            if (!_handlersMap.TryAdd(typeof(T), Handle)) {
                throw new ArgumentException($"Type {typeof(T).Name} already has a handler");
            }

            Task Handle(object evt, string pos, CancellationToken cancellationToken) 
                => evt is not T typed ? Task.CompletedTask : handler(typed, pos, cancellationToken);
        }

        public Task HandleEvent(object evt, string position, CancellationToken cancellationToken) =>
            !_handlersMap.TryGetValue(evt.GetType(), out var handler)
                ? Task.CompletedTask : handler(evt, position, cancellationToken);

        delegate Task HandleUntypedEvent(object evt, string position, CancellationToken cancellationToken);
    }

    public delegate Task HandleTypedEvent<in T>(T evt, string position, CancellationToken cancellationToken);
}