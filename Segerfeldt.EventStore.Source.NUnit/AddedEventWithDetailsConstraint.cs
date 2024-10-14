using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

/// <summary>The entity added events with specific details</summary>
/// <typeparam name="TDetails">The expected type of the details</typeparam>
/// <param name="name">The expected <see cref="UnpublishedEvent.Name"/></param>
/// <param name="details">The expected <see cref="UnpublishedEvent.Details"/></param>
public sealed class AddedEventWithDetailsConstraint<TDetails>(string name, TDetails details) : Constraint(name, details) where TDetails : class
{
    /// <inheritdoc/>
    public override string Description { get; } = $"added event with name '{name}' and details <{details}>";

    /// <inheritdoc/>
    public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
        this.GetConstraintResult(actual, entity => entity.UnpublishedEvents.Any(e => HasExpectedName(e) && HasExpectedDetails(e)));

    private bool HasExpectedName(UnpublishedEvent e) => e.Name == name;
    private bool HasExpectedDetails(UnpublishedEvent e) => e.Details.Equals(details);
}
