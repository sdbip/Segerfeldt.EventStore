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

        public (int? version, long? position) GetVersionAndPosition(string entityId)
        {
            var reader = connection.CreateCommand("SELECT (SELECT MAX(position) FROM Events)," +
                                                  " (SELECT MAX(version) FROM Events WHERE entity = @entityId)",
                    ("@entityId", entityId))
                .ExecuteReader();
            reader.Read();
            return (version: reader[1] as int?, position: reader[0] as long?);
        }

        public void InsertEntity(string id, int version)
        {
            var command = connection.CreateCommand(
                "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                ("@id", id),
                ("@version", version));
            command.ExecuteNonQuery();
        }

        public void UpdateEntityVersion(string id, int version)
        {
            connection.CreateCommand(
                    "UPDATE Entities SET version = @version WHERE id = @id",
                    ("(@id,", id),
                    ("@version", version))
                .ExecuteNonQuery();
        }

        internal void InsertEvent(string entityId, string eventName, string details, string actor, int version, long position)
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
