using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source.Tests
{
    internal static class ConnectionExtension
    {
        public static DbCommand CreateCommand(this DbConnection connection, string commandText)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            return command;
        }

        public static void AddParameter(this DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
