using Segerfeldt.EventStore.Source;

using System.Data.Common;
using System.Data.SqlClient;

namespace SourceConsoleApp;

internal sealed class SqlConnectionPool : IConnectionPool
{
    private readonly string connectionString;

    public SqlConnectionPool(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
