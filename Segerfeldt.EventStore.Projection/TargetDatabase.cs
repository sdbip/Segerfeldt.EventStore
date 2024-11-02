using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection;

public class TargetDatabase(Func<IDbConnection> connectionFactory)
{
    /// <summary>Creates a connection to the target database</summary>
    public IDbConnection CreateConnection() => connectionFactory.Invoke();

    /// <summary>Begins a transaction</summary>
    /// <exception cref="InvalidOperationException">If there is already a transaction in progress</exception>
    public Transaction BeginTransaction()
    {
        var connection = CreateConnection();
        connection.Open();
        return new Transaction(connection.BeginTransaction(), connection);
    }
}

/// <summary>An active database transaction</summary>
/// <param name="connection"></param>
public class Transaction(IDbTransaction transaction, IDbConnection connection)
{
    private readonly IDbTransaction transaction = transaction;
    private readonly IDbConnection connection = connection;

    /// <summary>Create a database command with a command text</summary>
    /// <param name="commandText">The command text to execute</param>
    public IDbCommand CreateCommand(string commandText) => connection.CreateCommand(commandText);

    internal void Commit()
    {
        try { transaction.Commit(); }
        finally { connection.Close(); }
    }

    internal void Rollback()
    {
        try { transaction.Rollback(); }
        finally { connection.Close(); }
    }

    public IDbTransaction GetDbTransaction() => transaction;
}
