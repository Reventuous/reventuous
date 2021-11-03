namespace Reventuous.Subscriptions.Redis
{
	public enum SubscriptionDroppedReason {
		/// <summary>
		/// Subscription was dropped because the subscription was disposed.
		/// </summary>
		Disposed,
		/// <summary>
		/// Subscription was dropped because of an error in user code.
		/// </summary>
		SubscriberError,
		/// <summary>
		/// Subscription was dropped because of a server error.
		/// </summary>
		ServerError
	}
}