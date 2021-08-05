using System.Data;

namespace Segerfeldt.EventStore.Source
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
