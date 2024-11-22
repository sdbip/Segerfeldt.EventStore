using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection;

public class TargetDatabase(Func<IDbConnection> connectionFactory)
{
    public IDbConnection SharedOpenConnection { get; set; } = null!;

    /// <summary>Creates a connection to the target database</summary>
    public IDbConnection CreateConnection() => connectionFactory.Invoke();

    public IDbConnection OpenSharedConnection()
    {
        SharedOpenConnection = CreateConnection();
        SharedOpenConnection.Open();
        return SharedOpenConnection;
    }

    public void CloseSharedConnection()
    {
        SharedOpenConnection.Close();
        SharedOpenConnection = null!;
    }
}
