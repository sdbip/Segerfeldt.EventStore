using System;
using System.Data;

namespace Segerfeldt.EventStore.Source
{
    internal sealed class AtomicOperation : IDisposable
    {
        private readonly IDbConnection connection;
        private IDbTransaction? transaction;

        private AtomicOperation(IDbConnection connection, IDbTransaction transaction)
        {
            this.transaction = transaction;
            this.connection = connection;
        }

        public static AtomicOperation BeginTransaction(IConnectionFactory connectionFactory)
        {
            var connection = connectionFactory.CreateConnection();
            connection.Open();
            var transaction = connection.BeginTransaction();

            return new AtomicOperation(connection, transaction);
        }

        public EntityVersion GetVersion(EntityId entityId)
        {
            var command = connection.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                ("@entityId", entityId.ToString()));
            return command.ExecuteScalar() is int versionValue
                ? EntityVersion.Of(versionValue)
                : EntityVersion.New;
        }

        public long? GetPosition() => connection.CreateCommand("SELECT MAX(position) FROM Events").ExecuteScalar() as long?;

        public void InsertEntity(EntityId id, EntityVersion version)
        {
            var command = connection.CreateCommand(
                "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                ("@id", id.ToString()),
                ("@version", version.Value));
            command.ExecuteNonQuery();
        }

        public void UpdateEntityVersion(EntityId id, EntityVersion version)
        {
            var command = connection.CreateCommand(
                "UPDATE Entities SET version = @version WHERE id = @id",
                ("(@id,", id.ToString()),
                ("@version", version.Value));
            command.ExecuteNonQuery();
        }

        internal void InsertEvent(EntityId entityId, string eventName, string details, string actor, EntityVersion version, long position)
        {
            connection.CreateCommand(
                    "INSERT INTO Events (entity, name, details, actor, version, position)" +
                    " VALUES (@entityId, @eventName, @details, @actor, @version, @position)",
                    ("@entityId", entityId.ToString()),
                    ("@eventName", eventName),
                    ("@details", details),
                    ("@actor", actor),
                    ("@version", version.Value),
                    ("@position", position))
                .ExecuteNonQuery();
        }

        public void Commit()
        {
            if (transaction is null) return;

            transaction?.Commit();
            connection.Close();
            transaction = null;
        }

        public void Rollback()
        {
            if (transaction is null) return;

            transaction?.Rollback();
            connection.Close();
            transaction = null;
        }

        public void Dispose()
        {
            Rollback();
        }
    }
}
