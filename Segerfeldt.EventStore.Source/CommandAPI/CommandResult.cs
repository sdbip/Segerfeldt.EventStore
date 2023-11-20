using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public class InvalidStatusCodeException : Exception
{
    public InvalidStatusCodeException(string? message) : base(message) { }
}

public interface ICommandResult
{
    public int StatusCode { get; }
    public object? Content { get; }
}

public static class CommandResultExtension
{
    public static ActionResult ActionResult(this ICommandResult result) =>
        result.Content is null
            ? new StatusCodeResult(result.StatusCode)
            : new ObjectResult(result.Content) { StatusCode = result.StatusCode };

    public static bool IsError(this ICommandResult commandResult) => IsErrorStatus(commandResult.StatusCode);

    internal static int GuardIsError(int statusCode)
    {
        if (!IsErrorStatus(statusCode))
            throw new InvalidStatusCodeException($"Status code {statusCode} is not an error code");
        return statusCode;
    }

    private static bool IsErrorStatus(int statusCode) => statusCode is < 200 or >= 300;

    public static CommandResult SameError(this ICommandResult commandResult) =>
        CommandResult.Error(commandResult.StatusCode, commandResult.Content);
    public static CommandResult<TOther> SameErrorFor<TOther>(this ICommandResult commandResult) =>
        new(commandResult.StatusCode, default, commandResult.Content);
}

public class CommandResult : ICommandResult
{
    public int StatusCode { get; }
    public object? Content { get; }

    private CommandResult(int statusCode, object? content)
    {
        StatusCode = statusCode;
        Content = content;
    }

    public static CommandResult Ok() => new(StatusCodes.Status204NoContent, null);
    public static CommandResult<T> Ok<T>(T value) => new(StatusCodes.Status200OK, value, value);

    public static CommandResult BadRequest(object error) => new(StatusCodes.Status400BadRequest, error);
    public static CommandResult NotFound(object error) => new(StatusCodes.Status404NotFound, error);

    public static CommandResult Unauthorized() => new(StatusCodes.Status401Unauthorized, null);
    public static CommandResult Unauthorized(object error) => new(StatusCodes.Status401Unauthorized, error);
    public static CommandResult Forbidden() => new(StatusCodes.Status403Forbidden, null);
    public static CommandResult Forbidden(object error) => new(StatusCodes.Status403Forbidden, error);

    public static CommandResult Error(int statusCode, object? content = null) =>
        new(CommandResultExtension.GuardIsError(statusCode), content);

    public static CommandResult<T> BadRequest<T>(object error) => Error(StatusCodes.Status400BadRequest, error);
    public static CommandResult<T> NotFound<T>(object error) => Error(StatusCodes.Status404NotFound, error);

    public static CommandResult<T> Unauthorized<T>() => Error(StatusCodes.Status401Unauthorized);
    public static CommandResult<T> Unauthorized<T>(object error) => Error(StatusCodes.Status401Unauthorized, error);
    public static CommandResult<T> Forbidden<T>() => Error(StatusCodes.Status403Forbidden);
    public static CommandResult<T> Forbidden<T>(object error) => Error(StatusCodes.Status403Forbidden, error);

    public static CommandResult<T> Error<T>(int statusCode, object? content = null) =>
      new(CommandResultExtension.GuardIsError(statusCode), default, content);
}

public class CommandResult<T> : ICommandResult
{
    public int StatusCode { get; }
    public object? Content { get; }
    public T? Value { get; }

    internal CommandResult(int statusCode, T? value, object? content)
    {
        Value = value;
        StatusCode = statusCode;
        Content = content;
    }

    public static implicit operator CommandResult<T>(CommandResult errorResult) =>
      new(errorResult.StatusCode, default, errorResult.Content);
}
