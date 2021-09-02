using System.Data.Common;

namespace Segerfeldt.EventStore.Source.Tests
{
    public class SingletonConnectionPool : IConnectionPool
    {
        private readonly DbConnection connection;

        public SingletonConnectionPool(DbConnection connection)
        {
            this.connection = connection;
        }

        public DbConnection CreateConnection() => connection;
    }
}
