using System;

namespace Reventuous.Subscriptions.Redis {
    static class RedisMappings {
        public static DropReason AsDropReason(SubscriptionDroppedReason reason)
            => reason switch {
                SubscriptionDroppedReason.Disposed => DropReason.Stopped,
                SubscriptionDroppedReason.ServerError => DropReason.ServerError,
                SubscriptionDroppedReason.SubscriberError => DropReason.SubscriptionError,
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
            };

    }
}