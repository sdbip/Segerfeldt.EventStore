using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using ProjectionWebApplication;
using ProjectionWebApplication.Schema;

using Segerfeldt.EventStore.Projection.MSSQL.Hosting;

using System.Data;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectionWebApplication", Version = "v1" });
});

builder.Services.AddSingleton<IDbConnection>(new SqlConnection(builder.Configuration.GetConnectionString("projection")));
builder.Services.AddSingleton<ScoreBoard>();
builder.Services.AddHostedSQLServerEventSource("events", builder.Configuration.GetConnectionString("events")!, new Segerfeldt.EventStore.Projection.Hosting.EventSourceOptions
{
    Initialization = p =>
    {
        var connection = p.GetRequiredService<IDbConnection>();
        Schema.CreateIfMissing(connection);
    }
})
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
