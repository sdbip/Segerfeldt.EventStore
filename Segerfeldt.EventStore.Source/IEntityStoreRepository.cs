using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

public interface IEntityStoreRepository
{
    Task<string?> GetTypeAsync(EntityId entityId, CancellationToken cancellationToken);
    Task<HistoryDAO?> GetHistoryAsync(EntityId entityId, EventOrdinal? after, DbTransaction? transaction, CancellationToken cancellationToken);
}

public readonly struct HistoryDAO
{
    public required string Type { get; init; }
    public required int Version { get; init; }
    public required PublishedEventDAO[] Events { get; init; }
}

public readonly struct PublishedEventDAO
{
    public required string Name { get; init;}
    public required string Details { get; init;}
    public required string Actor { get; init;}
    public required int Ordinal { get; init;}
    public required double Timestamp { get; init;}
}
