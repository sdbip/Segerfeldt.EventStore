using NUnit.Framework.Constraints;

using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

public sealed class AddedEventWithDetailsConstraint<TDetails>(string name, TDetails details) : Constraint(name, details) where TDetails : class
{
    public override string Description { get; } = $"added event with name '{name}' and details <{details}>";

    public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
        this.GetConstraintResult(actual, entity => entity.UnpublishedEvents.Any(e => HasExpectedName(e) && HasExpectedDetails(e)));

    private bool HasExpectedName(UnpublishedEvent e) => e.Name == name;
    private bool HasExpectedDetails(UnpublishedEvent e) => e.Details.Equals(details);
}
