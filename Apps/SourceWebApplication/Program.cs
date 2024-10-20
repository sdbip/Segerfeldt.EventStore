using System.Reflection;

using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.SQLite.CommandAPI;

using SourceWebApplication;

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

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options => {
    options.AddScheme<NaiveAuthenticationHandler>("", "");
});

// EventStore: A connection pool is needed to generate CommandContext for command handlers
builder.Services.UseSQLiteEventStore(builder.Configuration.GetConnectionString("main")!);

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
