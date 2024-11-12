using Microsoft.Extensions.DependencyInjection;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public class ProjectionEndpointConfiguration(IServiceCollection services)
{
    private readonly IServiceCollection services = services;

    public ProjectionEndpointConfiguration HideEntityType(EntityType type)
    {
        return this;
    }

    public ProjectionEndpointConfiguration HideEvent(string name, EntityType type)
    {
        return this;
    }
}
