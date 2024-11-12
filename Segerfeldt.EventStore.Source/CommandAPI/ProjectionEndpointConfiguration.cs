using System;
using System.Collections.Generic;
using System.Linq;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public class ProjectionEndpointConfiguration
{
    private readonly HashSet<EntityType> hiddenTypes = [];
    private readonly List<(EntityType, string)> hiddenEvents = [];

    public ProjectionEndpointConfiguration HideEntityType(EntityType type)
    {
        hiddenTypes.Add(type);
        return this;
    }

    public ProjectionEndpointConfiguration HideEvent(string name, EntityType type)
    {
        hiddenEvents.Add((type, name));
        return this;
    }

    public bool IsPublic(EventDAO dao) =>
        !hiddenTypes.Contains(dao.EntityType) &&
        !hiddenEvents.Contains((dao.EntityType, dao.Name));
}
