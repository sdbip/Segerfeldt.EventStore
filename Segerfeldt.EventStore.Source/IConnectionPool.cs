using System.Data.Common;

namespace Segerfeldt.EventStore.Source;

public interface IConnectionPool
{
    DbConnection CreateConnection();
}
