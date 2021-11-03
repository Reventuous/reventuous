using System;
using System.Collections;
using System.Collections.Generic;

namespace Reventuous.Subscriptions {
    public class DeserializationException : Exception {
        public DeserializationException(string stream, string eventType, string eventId, Exception e)
            : base($"Error deserializing event {stream} {eventId} {eventType}", e) {
        }
    }

    public class SubscriptionException : Exception {
        public SubscriptionException(string stream, string eventType, string eventId, object? evt, Exception e)
            : base($"Error processing event {stream} {eventId} {eventType}", e) {
            Data.Add("Event", evt);
        }

        public sealed override IDictionary Data { get; } = new Dictionary<string, object>();
    }
}