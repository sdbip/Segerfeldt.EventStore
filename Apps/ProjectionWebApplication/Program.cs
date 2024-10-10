using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using ProjectionWebApplication;

using Segerfeldt.EventStore.Projection.Hosting;

using System.Data.SqlClient;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectionWebApplication", Version = "v1" });
});

builder.Services.AddSingleton<ScoreBoard>();
builder.Services.AddSingleton<ProjectionTracker>();
builder.Services.AddHostedEventSource(new SqlConnection(builder.Configuration.GetConnectionString("events")!), "events")
    .AddReceptacles(Assembly.GetExecutingAssembly())
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
