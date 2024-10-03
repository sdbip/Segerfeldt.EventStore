using System;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class ParseException : Exception
{
    public object? ErrorData { get; }

    public ParseException(string message, object? errorData = null) : base(message)
    {
        ErrorData = errorData;
    }
}
