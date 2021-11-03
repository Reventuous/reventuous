using System;
using StackExchange.Redis;
using JetBrains.Annotations;

namespace Reventuous.Subscriptions.Redis {
    public abstract class RedisSubscriptionOptions : SubscriptionOptions {
        /// <summary>
        /// Optional function to configure client operation options
        /// </summary>
        public Action<ConfigurationOptions>? ConfigureOperation { get; init; }
        /// <summary>
        /// StackExchange.Redis implementation of XREAD and XREADGROUP is not blocking, so it requires to pull constantly to get new messages.
        /// By default, it will check for new messages every 1 second.
        /// </summary>
        public int ReadDelay { get; init; } = 1000; 

    }

    public class StreamSubscriptionOptions : RedisSubscriptionOptions {
        public string StreamName { get; init; } = null!;
        /// <summary>
        /// Number of messages to read in one go from the stream
        /// </summary>
        public int NumberOfMessages { get; init; } = 1;
    }

    public class AllStreamSubscriptionOptions : RedisSubscriptionOptions { 
        public string StreamName { get; init; } = "$all";
        public int NumberOfMessages { get; init; } = 1;
    }

    [PublicAPI]
    public class StreamPersistentSubscriptionOptions : RedisSubscriptionOptions {
        public string StreamName { get; init; } = null!;
        public string ConsumerId { get; init; } = null!;

        /// <summary>
        /// Acknowledge events without an explicit ACK
        /// </summary>
        public bool AutoAck { get; init; } = true;
    }
}