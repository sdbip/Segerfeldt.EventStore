using System.Data;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal static class ConnectionExtension
    {
        public static IDbCommand CreateCommand(this IDbTransaction transaction, string commandText, params (string name, object? value)[] parameters)
        {
            var command = transaction.Connection!.CreateCommand(commandText, parameters);
            command.Transaction = transaction;
            return command;
        }

        public static DbCommand CreateCommand(this DbTransaction transaction, string commandText, params (string name, object? value)[] parameters)
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

        public static DbCommand CreateCommand(this DbConnection connection, string commandText, params (string name, object? value)[] parameters)
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
