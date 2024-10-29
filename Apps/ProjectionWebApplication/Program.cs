using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using ProjectionWebApplication;
using ProjectionWebApplication.Schema;

using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Projection.Hosting;
using Segerfeldt.EventStore.Projection.MSSQL.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectionWebApplication", Version = "v1" });
});

builder.Services.AddSingleton<ScoreBoard>();
builder.Services.AddHostedSQLServerEventSource("events", builder.Configuration.GetConnectionString("events")!, new EventSourceOptions
{
    Initialization = p =>
    {
        var connection = p.GetRequiredService<TargetDbConnection>().CreateNonTransactional();
        Schema.CreateIfMissing(connection);
    }
})
    .SetSQLServerTarget(builder.Configuration.GetConnectionString("projection")!)
    .AddReceptacles()
    .SetProjectionTracker<ProjectionTracker>();

var app = builder.Build();
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectionWebApplication v1"));
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
