namespace Segerfeldt.EventStore.Projection.SQLite.Tests;

internal sealed class Trap<T>
{
    public T? Value { get; set; }
}
