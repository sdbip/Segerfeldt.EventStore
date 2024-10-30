using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection;

/// <summary>An active database transaction</summary>
/// <param name="connection"></param>
public class Transaction(IDbConnection connection)
{
    private readonly IDbConnection connection = connection;

    /// <summary>Create a database command with a command text</summary>
    /// <param name="commandText">The command text to execute</param>
    public IDbCommand CreateCommand(string commandText) => connection.CreateCommand(commandText);

    internal void Commit()
    {
        try { connection.CreateCommand("COMMIT").ExecuteNonQuery(); }
        finally { connection.Close(); }
    }

    internal void Rollback()
    {
        try { connection.CreateCommand("ROLLBACK").ExecuteNonQuery(); }
        finally { connection.Close(); }
    }
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
        try { Transaction?.Commit(); }
        finally { Transaction = null; }
    }

    /// <summary>Rolls back and ends the current transaction</summary>
    /// <exception cref="InvalidOperationException">If there is no transaction in progress</exception>
    public void Rollback()
    {
        try { Transaction?.Rollback(); }
        finally { Transaction = null; }
    }
}
