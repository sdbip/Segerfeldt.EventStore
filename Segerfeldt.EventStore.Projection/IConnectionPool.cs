using System.Data;

namespace Segerfeldt.EventStore.Projection;

public interface IConnectionPool
{
    IDbConnection CreateConnection();
}
