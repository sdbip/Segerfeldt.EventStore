using Segerfeldt.EventStore.Projection;

using System.Data;
using System.Data.SqlClient;

namespace ProjectionWebApplication;

public sealed class SqlConnectionPool(string connectionString) : IConnectionPool
{
    private readonly string connectionString = connectionString;

    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
