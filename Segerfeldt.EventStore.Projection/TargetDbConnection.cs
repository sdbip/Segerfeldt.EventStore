using Segerfeldt.EventStore.Shared;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection;

public class TargetDbConnection(IDbConnection connection)
{
    public IDbConnection WithoutTransaction => transaction is null ? connection : throw new InvalidOperationException("A transaction is in progress");
    private IDbTransaction? transaction;


    public void BeginTransaction()
    {
        if (transaction is not null) throw new InvalidOperationException("There is already a transaction in progress");
        connection.Open();
        transaction = connection.BeginTransaction();
    }

    public IDbCommand CreateCommand(string commandText) => transaction?.CreateCommand(commandText) ?? connection.CreateCommand(commandText);

    public void Commit()
    {
        if (transaction is null) throw new InvalidOperationException("There is no transaction in progress");
        try { transaction.Commit(); }
        finally { transaction = null; connection.Close(); }
    }

    public void Rollback()
    {
        if (transaction is null) throw new InvalidOperationException("There is no transaction in progress");
        try { transaction.Rollback(); }
        finally { transaction = null; connection.Close(); }
    }
}
