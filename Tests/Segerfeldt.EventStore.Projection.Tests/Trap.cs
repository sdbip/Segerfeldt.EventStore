namespace Segerfeldt.EventStore.Projection.Tests;

internal sealed class Trap<T>
{
    public T? Value { get; set; }
}
