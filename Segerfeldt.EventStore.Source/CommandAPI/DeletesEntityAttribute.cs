using Microsoft.OpenApi.Models;

namespace Segerfeldt.EventStore.Source.CommandAPI;

/// <summary>
/// Annotate a command handler to generate a RESTful HTTP endpoint that deletes an entity (or a child).
/// Typical form: DELETE /entity/{entityId}
/// Alternate form: DELETE /entity/{entityId}/property/{propertyId}
/// </summary>
public sealed class DeletesEntityAttribute : ModifiesEntityAttribute
{
    public DeletesEntityAttribute(string entity) : base(entity, OperationType.Delete)
    {
        EntityId = DefaultEntityId;
    }
}
