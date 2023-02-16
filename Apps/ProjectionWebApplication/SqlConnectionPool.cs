using Segerfeldt.EventStore.Projection;

using System.Data;
using System.Data.SqlClient;

namespace ProjectionWebApplication;

public class SqlConnectionPool : IConnectionPool
{
    private readonly string connectionString;

    public SqlConnectionPool(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
