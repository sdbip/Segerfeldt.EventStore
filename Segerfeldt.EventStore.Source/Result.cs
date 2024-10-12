using System;

namespace Segerfeldt.EventStore.Source;

public readonly struct Result<T>(T? value, Exception? error)
{
    private readonly T? value = value;
    private readonly Exception? error = error;

    public readonly bool IsFailure => value == null;
    public readonly bool IsSuccess => !IsFailure;

    public Result(T value) : this(value, null) { }
    public Result(Exception error) : this(default, error) { }

    public static implicit operator Result<T>(T value) => new(value, null);

    public Result<U> IfSuccess<U>(Func<T, Result<U>> conversion)
    {
        if (IsFailure) return new(default, error);
        else return conversion(value!);
    }

    public T OrThrow()
    {
        if (value != null) return value;
        throw error ?? new Exception("Operation Failed");
    }
}
