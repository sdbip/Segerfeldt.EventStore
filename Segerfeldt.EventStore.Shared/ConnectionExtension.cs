using System;
using System.Data;
using System.Data.Common;

namespace Segerfeldt.EventStore.Shared;

internal static class ConnectionExtension
{
    public static IDbCommand CreateCommand(this IDbTransaction transaction, string commandText)
    {
        var command = transaction.Connection!.CreateCommand(commandText);
        command.Transaction = transaction;
        return command;
    }

    public static DbCommand CreateCommand(this DbTransaction transaction, string commandText)
    {
        var command = transaction.Connection!.CreateCommand(commandText);
        command.Transaction = transaction;
        return command;
    }

    public static IDbCommand CreateCommand(this IDbConnection connection, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        return command;
    }

    public static DbCommand CreateCommand(this DbConnection connection, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        return command;
    }

    public static void AddParameter(this IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    public static void AddParameter(this DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
