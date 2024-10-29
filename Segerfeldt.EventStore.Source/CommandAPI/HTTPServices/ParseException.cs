using System;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal sealed class ParseException(string message, object? errorData = null) : Exception(message)
{
    public object? ErrorData { get; } = errorData;
}
