using System.Threading;
using System.Threading.Tasks;

namespace Reventuous.Subscriptions {
    public interface IEventHandler {
        string SubscriptionId { get; }
        
        Task HandleEvent(object evt, string position, CancellationToken cancellationToken);
    }
}
