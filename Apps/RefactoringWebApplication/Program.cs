using System.Reflection;

using RefactoringWebApplication;

using Segerfeldt.EventStore.Refactoring.SQLite;
using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.SQLite.CommandAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvcCore();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
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

// EventStore: Set up the event store
builder.Services.UseSQLiteEventStore(builder.Configuration.GetConnectionString("main")!);

// EventStore: Set up the refactoring transformation
builder.Services.UseSQLiteRefactoring(builder.Configuration.GetConnectionString("main")!)
    .UseSQLiteTarget(builder.Configuration.GetConnectionString("transformed")!)
    .UseProjectionTracker<ProjectionTracker>()
    .UseTransformationStrategy<TransformationStrategy>();

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
app.MapControllers();
app.MapCommands(Assembly.GetExecutingAssembly());

app.Run();
