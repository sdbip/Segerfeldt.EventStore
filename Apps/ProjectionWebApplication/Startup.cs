using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Projection.Hosting;

using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace ProjectionWebApplication;

public class Startup
{
    private readonly IConfiguration configuration;

    public Startup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectionWebApplication", Version = "v1" });
        });

        services.AddSingleton<ScoreBoard>();
        services.AddSingleton<PositionTracker>();
        services.AddHostedEventSource(new SqlConnectionPool(configuration.GetConnectionString("events")))
            .AddReceptacles(Assembly.GetExecutingAssembly())
            .SetPositionTracker<PositionTracker>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectionWebApplication v1"));
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}

public class SqlConnectionPool : IConnectionPool
{
    private readonly string connectionString;

    public SqlConnectionPool(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
