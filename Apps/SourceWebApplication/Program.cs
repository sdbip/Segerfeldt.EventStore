using System.Data.Common;
using System.Reflection;

using Microsoft.Data.Sqlite;

using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.SQLite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvcCore();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // EventStore: Add Commands to Swagger documentation
    options.DocumentCommands(Assembly.GetExecutingAssembly());
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "SourceWebApplication.xml"));
});

// EventStore: A connection pool is needed to generate CommandContext for command handlers
builder.Services.AddSingleton<IConnectionFactory>(p =>
{
    var connection = new SqliteConnection(builder.Configuration.GetConnectionString("main"));
    Schema.CreateIfMissing(connection);
    return new MainConnectionFactory(builder.Configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// EventStore: Map command-handlers
app.MapCommands(Assembly.GetExecutingAssembly());

app.Run();

internal class MainConnectionFactory(IConfiguration configuration) : IConnectionFactory
{
    private readonly IConfiguration configuration = configuration;

    public DbConnection CreateConnection() => new SqliteConnection(configuration.GetConnectionString("main"));
}
