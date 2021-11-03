using System.Threading;
using System.Threading.Tasks;

namespace Reventuous.Subscriptions {
    public record Checkpoint(string SubscriptionId, string Position);

    public interface ICheckpointStore {
        ValueTask<Checkpoint> GetLastCheckpoint(string checkpointId, CancellationToken cancellationToken = default);

        ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, CancellationToken cancellationToken = default);
    }
}
