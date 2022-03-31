using JetBrains.Annotations;

namespace Segerfeldt.EventStore.Source.NUnit;

[PublicAPI]
public static class Added
{
    public static readonly AddedNoEventsConstraint NoEvents = new();
    public static AddedEventConstraint Event(string name)  => new(name);
}
