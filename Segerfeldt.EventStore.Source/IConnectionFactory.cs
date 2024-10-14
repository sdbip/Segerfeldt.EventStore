using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source;

/// <summary>An object that can create connections to the write-model database</summary>
public interface IConnectionFactory
{
    /// <summary>Create (but don't open) a new connection to the write-model database</summary>
    /// <returns>a closed connection</returns>
    DbConnection CreateConnection();
}

internal class OnDemandConnectionFactory(Func<DbConnection> createConnection) : IConnectionFactory
{
    public DbConnection CreateConnection() => createConnection();
}
