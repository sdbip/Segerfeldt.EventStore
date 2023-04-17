using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class QueryHandler
{
    private readonly HttpContext context;
    private readonly ServiceLocator serviceLocator;

    public QueryHandler(HttpContext context)
    {
        this.context = context;
        serviceLocator = new ServiceLocator(context.RequestServices);
    }

    public async Task<ActionResult> GetHistory()
    {
        var id = (string?)context.GetRouteValue("entityId");
        var store = serviceLocator.GetServiceOrCreateInstance<EntityStore>();

        var history = await store.GetHistoryAsync(new EntityId(id!));
        if (history is null)
            return new NotFoundObjectResult($"There is no entity with the id '{id}'");
        else
            return new OkObjectResult(History.From(history));
    }
}
