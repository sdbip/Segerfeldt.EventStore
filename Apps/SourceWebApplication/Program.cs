using System.Data.Common;
using System.Reflection;

using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

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
builder.Services.AddSingleton<IConnectionPool, NonsenseConnectionPool>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapCommands(Assembly.GetExecutingAssembly());
});

app.Run();

internal class NonsenseConnectionPool : IConnectionPool
{
    public DbConnection CreateConnection() => throw new NotImplementedException("This connection pool is not meant to be used.");
}
