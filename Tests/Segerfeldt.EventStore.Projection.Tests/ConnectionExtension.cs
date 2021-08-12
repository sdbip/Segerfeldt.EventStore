using System.Data;

namespace Segerfeldt.EventStore.Projection.Tests
{
    internal static class ConnectionExtension
    {
        public static IDbCommand CreateCommand(this IDbTransaction transaction, string commandText, params (string name, object? value)[] parameters)
        {
            var command = transaction.Connection!.CreateCommand(commandText, parameters);
            command.Transaction = transaction;
            return command;
        }

        public static IDbCommand CreateCommand(this IDbConnection connection, string commandText, params (string name, object? value)[] parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;

            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }

            return command;
        }
    }
}
