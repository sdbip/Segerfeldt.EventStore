using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.Tests;

internal static class ConnectionExtension
{
    public static IDbCommand CreateCommand(this IDbConnection connection, string commandText)
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
}
