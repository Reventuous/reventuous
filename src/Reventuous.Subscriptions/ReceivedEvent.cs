using System;

namespace Reventuous.Subscriptions {
    public record ReceivedEvent(
        string               EventId,
        string               EventType,
        string               ContentType,
        string               Stream,
        string               Position,
        DateTime             Created,
        object?              Payload
    );
}