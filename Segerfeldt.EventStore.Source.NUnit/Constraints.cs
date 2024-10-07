using NUnit.Framework.Constraints;

using System;
using System.Linq;

namespace Segerfeldt.EventStore.Source.NUnit;

internal static class Constraints
{
    public static ConstraintResult GetConstraintResult(this IConstraint constraint, object? actual, Func<IEntity, bool> isSuccess)
    {
        if (actual is not IEntity entity) return new ConstraintResult(constraint, actual, ConstraintStatus.Error);
        var actualValue = entity.UnpublishedEvents.Select(e => new { e.Name, e.Details });
        return new ConstraintResult(constraint, actualValue, isSuccess(entity));
    }
}
