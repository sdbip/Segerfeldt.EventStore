using System.Collections.Generic;
using System.Text.Json;

namespace Segerfeldt.EventStore.Source.CommandAPI.DTOs;

public record ProjectionPosition(long Position, IEnumerable<ProjectionEvent> Events);
public record Entity(string Id, string Type);
public record ProjectionEvent(Entity entity, string Name, JsonElement Details);
