using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source.PostgreSQL.CommandAPI;

public static class Commanding
{
    /// <summary>Add a EventStore write-model stored in a PostgreSQL database</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param  name="connectionString">the connection-string to access the database</param>
    public static IServiceCollection UsePostgreSQLEventStore(this IServiceCollection services, string connectionString, EventStoreOptions? options = null)
    {
        return services.UseEventStore(_ => CreateConnection(), options ?? new EventStoreOptions { PrepareDatabase = _ => Schema.CreateIfMissing(CreateConnection()) });

        DbConnection CreateConnection() => new NpgsqlConnection(connectionString);
    }
}
