using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal sealed class ProjectionQueryRequest(HttpContext context)
{
    private readonly HttpContext context = context;
    private readonly IServiceProvider serviceProvider = context.RequestServices;

    public async Task<ActionResult> GetAsync()
    {
        var config = serviceProvider.GetRequiredService<ProjectionEndpointConfiguration>();
        var afterString = context.Request.Query["after"].FirstOrDefault();
        var after = string.IsNullOrEmpty(afterString) ? (long?)null : long.Parse(afterString);
        var repository = serviceProvider.GetService<IProjectionRepository>()
            ?? ActivatorUtilities.CreateInstance<StandardCompliantProjectionRepository>(serviceProvider);
        var events = await repository.GetEventsAsync(after, 100, default);
        var grouped = events.Where(config.IsPublic).GroupBy(e => e.Position).ToList();
        if (grouped.Count > 1 && grouped.SelectMany(g => g).Count() > 100)
            grouped.RemoveAt(grouped.Count - 1);

        return new OkObjectResult(grouped.Select(g => new
        {
            position = g.Key,
            events = g.OrderBy(e => e.Position).Select(e => new
            {
                entity = new { id = e.EntityId.ToString(), type = e.EntityType.ToString() },
                name = e.Name,
                details = e.Details,
            }),
        }));
    }
}
