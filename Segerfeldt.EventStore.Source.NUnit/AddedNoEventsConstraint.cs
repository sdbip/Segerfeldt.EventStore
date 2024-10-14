using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

/// <summary>The entity has added no events whatsoever</summary>
public sealed class AddedNoEventsConstraint : Constraint
{
    /// <inheritdoc/>
    public override string Description => "no added events";

    /// <inheritdoc/>
    public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
        this.GetConstraintResult(
            actual,
            entity => !entity.UnpublishedEvents.Any());
}
