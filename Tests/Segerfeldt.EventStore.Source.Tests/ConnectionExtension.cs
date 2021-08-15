using System.Data.Common;

namespace Segerfeldt.EventStore.Source.Tests
{
    internal static class ConnectionExtension
    {
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
