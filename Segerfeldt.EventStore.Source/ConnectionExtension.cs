using System.Data;

namespace Segerfeldt.EventStore.Source
{
    public static class ConnectionExtension
    {
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

        internal static void InsertEvent(this IDbConnection connection, string entityId, string eventName, string details, string actor, int version, long position)
        {
            connection.CreateCommand(
                    "INSERT INTO Events (entity, name, details, actor, version, position)" +
                    " VALUES (@entityId, @eventName, @details, @actor, @version, @position)",
                    ("@entityId", entityId),
                    ("@eventName", eventName),
                    ("@details", details),
                    ("@actor", actor),
                    ("@version", version),
                    ("@position", position))
                .ExecuteNonQuery();
        }

        internal static (int? version, long? position) GetVersionAndPosition(this IDbConnection connection, string entityId)
        {
            var reader = connection.CreateCommand("SELECT (SELECT MAX(position) FROM Events)," +
                                                  " (SELECT MAX(version) FROM Events WHERE entity = @entityId)",
                    ("@entityId", entityId.ToString()))
                .ExecuteReader();
            reader.Read();
            return (version: reader[1] as int?, position: reader[0] as long?);
        }

        internal static void InsertEntity(this IDbConnection connection, string id, int version)
        {
            var command = connection.CreateCommand(
                "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                ("@id", id),
                ("@version", version));
            command.ExecuteNonQuery();
        }

        internal static void UpdateEntityVersion(this IDbConnection connection, string id, int version)
        {
            connection.CreateCommand(
                    "UPDATE Entities SET version = @version WHERE id = @id",
                    ("(@id,", id),
                    ("@version", version))
                .ExecuteNonQuery();
        }
    }
}
