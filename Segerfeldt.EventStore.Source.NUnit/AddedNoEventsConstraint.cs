using NUnit.Framework.Constraints;

using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

public sealed class AddedNoEventsConstraint : Constraint
{
    public override string Description => "no added events";

    public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
        this.GetConstraintResult(
            actual,
            entity => !entity.UnpublishedEvents.Any());
}
