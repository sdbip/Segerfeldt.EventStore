using NUnit.Framework.Constraints;

using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

public sealed class AddedNoEventsConstraint : Constraint
{
    public AddedNoEventsConstraint() => Description = "no added events";

    public override ConstraintResult ApplyTo<TActual>(TActual actual) => this.GetConstraintResult(actual,
        entity => !entity.UnpublishedEvents.Any());
}
