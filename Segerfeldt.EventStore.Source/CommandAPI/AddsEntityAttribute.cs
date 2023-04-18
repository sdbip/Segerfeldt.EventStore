using Microsoft.OpenApi.Models;

namespace Segerfeldt.EventStore.Source.CommandAPI;

/// <summary>
/// Annotate a command handler to generate a RESTful HTTP endpoint that adds an entity (or a child).
/// Typical form: POST /entity
/// Alternate form: POST /entity/{entityId}/property
/// </summary>
public sealed class AddsEntityAttribute : ModifiesEntityAttribute
{
    public AddsEntityAttribute(string entity) : base(entity, OperationType.Post) { }
}
