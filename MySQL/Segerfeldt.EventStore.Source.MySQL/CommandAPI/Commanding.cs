using Microsoft.Extensions.DependencyInjection;

using MySql.Data.MySqlClient;

using Segerfeldt.EventStore.Source.CommandAPI;

using System.Data.Common;

namespace Segerfeldt.EventStore.Source.MySQL.CommandAPI;

public static class Commanding
{
    /// <summary>Add a EventStore write-model stored in a MySQL database</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param  name="connectionString">the connection-string to access the database</param>
    public static IServiceCollection UseMySQLEventStore(this IServiceCollection services, string connectionString, EventStoreOptions? options = null)
    {
        return services.UseEventStore(_ => CreateConnection(), options ?? new EventStoreOptions { PrepareDatabase = _ => Schema.CreateIfMissing(CreateConnection()) });

        DbConnection CreateConnection() => new MySqlConnection(connectionString);
    }
}
