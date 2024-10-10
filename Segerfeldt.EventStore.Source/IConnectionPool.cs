using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source;

public interface IConnectionPool
{
    DbConnection CreateConnection();
}

internal class OnDemandConnectionFactory(Func<DbConnection> createConnection) : IConnectionPool
{
    public DbConnection CreateConnection() => createConnection();
}
