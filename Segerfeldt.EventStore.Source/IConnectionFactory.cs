using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source;

public interface IConnectionFactory
{
    DbConnection CreateConnection();
}

internal class OnDemandConnectionFactory(Func<DbConnection> createConnection) : IConnectionFactory
{
    public DbConnection CreateConnection() => createConnection();
}
