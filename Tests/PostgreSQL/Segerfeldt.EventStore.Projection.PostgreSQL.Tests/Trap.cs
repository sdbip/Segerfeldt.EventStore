namespace Segerfeldt.EventStore.Projection.PostgreSQL.Tests;

internal sealed class Trap<T>
{
    public T? Value { get; set; }
}
