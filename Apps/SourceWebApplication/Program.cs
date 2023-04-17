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
    options.DocumentCommands(Assembly.GetExecutingAssembly());
});

builder.Services.AddSingleton<IConnectionPool, NonsenseConnectionPool>(); // Needed to generate CommandContext

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
    public DbConnection CreateConnection() => null!;
}
