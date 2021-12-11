using NUnit.Framework.Constraints;

using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit
{
    public sealed class AddedEventWithDetailsConstraint<TDetails> : Constraint where TDetails : class
    {
        private string NameArgument => (string)Arguments[0];
        private TDetails DetailsArgument => (TDetails)Arguments[1];

        public AddedEventWithDetailsConstraint(string name, TDetails details) : base(name, details) =>
            Description = $"added event with name '{name}' and details <{details}>";

        public override ConstraintResult ApplyTo<TActual>(TActual actual) =>
            this.GetConstraintResult(actual, entity => entity.UnpublishedEvents.Any(e => HasExpectedName(e) && HasExpectedDetails(e)));

        private bool HasExpectedName(UnpublishedEvent e) => e.Name == NameArgument;
        private bool HasExpectedDetails(UnpublishedEvent e) => e.Details.Equals(DetailsArgument);
    }
}
