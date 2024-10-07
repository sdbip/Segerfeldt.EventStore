using NUnit.Framework.Constraints;

using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

public sealed class AddedEventConstraint(string name) : Constraint(name)
{
    public override string Description { get; } = $"added event with name '{name}'";

    public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
        this.GetConstraintResult(actual,
            entity => entity.UnpublishedEvents.Any(HasExpectedName));

    public AddedEventWithDetailsConstraint<TDetails> WithDetails<TDetails>(TDetails details)
        where TDetails : class => new(name, details);

    private bool HasExpectedName(UnpublishedEvent e) => e.Name == name;
}
