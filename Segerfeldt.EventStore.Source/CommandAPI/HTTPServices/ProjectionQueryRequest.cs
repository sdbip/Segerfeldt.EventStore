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
        var repository = serviceProvider.GetService<IProjectionRepository>()
            ?? ActivatorUtilities.CreateInstance<StandardCompliantProjectionRepository>(serviceProvider);

        var after = GetQueryValue("after", long.Parse);
        var maxCount = GetQueryValue("maxCount", int.Parse) ?? 100;
        var events = await repository.GetEventsAsync(after, maxCount, default);
        return new OkObjectResult(events.Where(config.IsPublic));
    }

    private T? GetQueryValue<T>(string key, Func<string, T> parse) where T : struct
    {
        var str = context.Request.Query[key].FirstOrDefault();
        return string.IsNullOrEmpty(str) ? null : parse(str);
    }
}
