using NUnit.Framework.Constraints;

using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

public sealed class AddedEventConstraint : Constraint
{
    private string NameArgument => (string)Arguments[0];

    public AddedEventConstraint(string name) : base(name) => Description = $"added event with name '{name}'";

    public override ConstraintResult ApplyTo<TActual>(TActual actual) => this.GetConstraintResult(actual,
        entity => entity.UnpublishedEvents.Any(HasExpectedName));

    public AddedEventWithDetailsConstraint<TDetails> WithDetails<TDetails>(TDetails details)
        where TDetails : class => new(NameArgument, details);

    private bool HasExpectedName(UnpublishedEvent e) => e.Name == NameArgument;
}
