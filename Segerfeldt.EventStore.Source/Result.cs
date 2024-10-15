using System;

namespace Segerfeldt.EventStore.Source;

/// <summary>The result of an operation that may succeed or fail</summary>
/// <typeparam name="T">The type of the value if successful</typeparam>
public readonly struct Result<T>
{
    private readonly T? value;
    private readonly Exception? error;

    /// <summary>Whether this result is a failur</summary>
    public readonly bool IsFailure => value is null;
    /// <summary>Whether this result is successful</summary>
    public readonly bool IsSuccess => !IsFailure;

    private Result(T? value, Exception? error)
    {
        this.value = value;
        this.error = error;
    }

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(Failure failure) => new(default, failure.error);

    /// <summary>A successful result</summary>
    /// <param name="value">The resulting value</param>
    public static Result<T> Success(T value) => new(value, null);
    /// <summary>A failed result</summary>
    /// <param name="error">An exception that explains the error</param>
    public static Result<T> Failure(Exception error) => new(default, error);

    public Result<U> IfSuccess<U>(Func<T, Result<U>> conversion)
    {
        if (IsFailure) return new(default, error);
        else return conversion(value!);
    }

    /// <summary>Get the value or throw the error <see cref="Exception"/></summary>
    /// <exception cref="Exception">Thrown if failure</exception>
    /// <returns>The value</returns>
    public T OrThrow() => value ?? throw error ?? new Exception("Operation Failed");
}

/// <summary>A result which is always a failure</summary>
public readonly struct Failure
{
    internal readonly Exception error;

    private Failure(Exception error) => this.error = error;

    /// <summary>A failure</summary>
    /// <param name="error">An exception that explains the error</param>
    public static Failure Error(Exception error) => new(error);
}
