using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection;

/// <summary>An active database transaction</summary>
/// <param name="connection"></param>
public class Transaction(IDbConnection connection)
{
    /// <summary>The connection that is participating in the transaction</summary>
    public IDbConnection Connection { get; } = connection;
}

public class TargetDatabase(Func<IDbConnection> connectionFactory)
{
    /// <summary>The active transaction to the target database, null if not currently emitting</summary>
    public Transaction? Transaction { get; private set; }
    /// <summary>Creates a connection to the target database</summary>
    public IDbConnection CreateConnection() => connectionFactory.Invoke();

    /// <summary>Begins a transaction</summary>
    /// <exception cref="InvalidOperationException">If there is already a transaction in progress</exception>
    public void BeginTransaction()
    {
        if (Transaction is not null) throw new InvalidOperationException("There is already a transaction in progress");
        var connection = CreateConnection();
        Transaction = new Transaction(connection);

        connection.Open();
        connection.CreateCommand("BEGIN TRANSACTION").ExecuteNonQuery();
    }

    /// <summary>Commits and ends the current transaction</summary>
    /// <exception cref="InvalidOperationException">If there is no transaction in progress</exception>
    public void Commit()
    {
        if (Transaction is null) throw new InvalidOperationException("There is no transaction in progress");
        try { Transaction.Connection.CreateCommand("COMMIT").ExecuteNonQuery(); }
        finally { Transaction.Connection.Close(); Transaction = null; }
    }

    /// <summary>Rolls back and ends the current transaction</summary>
    /// <exception cref="InvalidOperationException">If there is no transaction in progress</exception>
    public void Rollback()
    {
        if (Transaction is null) throw new InvalidOperationException("There is no transaction in progress");
        try { Transaction.Connection.CreateCommand("ROLLBACK").ExecuteNonQuery(); }
        finally { Transaction.Connection.Close(); Transaction = null; }
    }
}
