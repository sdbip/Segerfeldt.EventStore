using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class HistoryQueryRequest(HttpContext context)
{
    private readonly HttpContext context = context;

    public async Task<ActionResult> Get()
    {
        var id = (string?)context.GetRouteValue("entityId");
        var store = new EntityStore(context.RequestServices.GetRequiredService<IConnectionFactory>());

        var history = await store.GetHistoryAsync(new EntityId(id!));
        if (history is null)
            return new NotFoundObjectResult($"There is no entity with the id '{id}'");
        else
            return new OkObjectResult(History.From(history));
    }
}
