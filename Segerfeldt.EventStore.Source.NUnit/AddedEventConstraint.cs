using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

/// <summary>The entity added events with specific details</summary>
/// <param name="name">The expected <see cref="UnpublishedEvent.Name"/></param>
public sealed class AddedEventConstraint(string name) : Constraint(name)
{
    /// <inheritdoc/>
    public override string Description { get; } = $"added event with name '{name}'";

    /// <inheritdoc/>
    public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
        this.GetConstraintResult(actual,
            entity => entity.UnpublishedEvents.Any(HasExpectedName));

    /// <summary>Add details to the constraint</summary>
    /// <typeparam name="TDetails">The expected type of the details</typeparam>
    /// <param name="details">The expected <see cref="UnpublishedEvent.Details"/></param>
    public AddedEventWithDetailsConstraint<TDetails> WithDetails<TDetails>(TDetails details)
        where TDetails : class => new(name, details);

    private bool HasExpectedName(UnpublishedEvent e) => e.Name == name;
}
