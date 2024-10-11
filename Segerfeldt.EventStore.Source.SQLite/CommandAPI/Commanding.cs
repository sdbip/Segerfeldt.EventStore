using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI;

namespace Segerfeldt.EventStore.Source.SQLite.CommandAPI;

public static class Commanding
{
    /// <summary>Add a EventStore write-model stored in a SQLite database</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param  name="connectionString">the connection-string to access the database</param>
    public static IServiceCollection UseSQLiteEventStore(this IServiceCollection services, string connectionString) =>
        services.UseEventStore(new SQLiteEventStoreProvider(connectionString));
}
