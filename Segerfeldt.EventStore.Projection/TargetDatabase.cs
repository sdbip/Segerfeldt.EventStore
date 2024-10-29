using Segerfeldt.EventStore.Shared;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection;

public class TargetDatabase(Func<IDbConnection> connectionFactory)
{
    private readonly IDbConnection transactionalConnection = connectionFactory.Invoke();
    private IDbTransaction? transaction;

    public IDbConnection CreateConnection() => connectionFactory.Invoke();

    public void BeginTransaction()
    {
        if (transaction is not null) throw new InvalidOperationException("There is already a transaction in progress");
        transactionalConnection.Open();
        transaction = transactionalConnection.BeginTransaction();
    }

    public IDbCommand CreateCommand(string commandText) => transaction?.CreateCommand(commandText) ?? transactionalConnection.CreateCommand(commandText);

    public void Commit()
    {
        if (transaction is null) throw new InvalidOperationException("There is no transaction in progress");
        try { transaction.Commit(); }
        finally { transaction = null; transactionalConnection.Close(); }
    }

    public void Rollback()
    {
        if (transaction is null) throw new InvalidOperationException("There is no transaction in progress");
        try { transaction.Rollback(); }
        finally { transaction = null; transactionalConnection.Close(); }
    }
}
