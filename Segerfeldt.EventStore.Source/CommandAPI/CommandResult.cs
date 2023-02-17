using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI;

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

    public static bool IsError(this ICommandResult commandResult) => commandResult.StatusCode / 100 != 2;

    public static CommandResult SameError(this ICommandResult commandResult)
    {
        if (!commandResult.IsError()) throw new Exception("Successful result used as error");
        return CommandResult.Error(commandResult);
    }

    public static CommandResult<TOther> SameErrorFor<TOther>(this ICommandResult commandResult)
    {
        return CommandResult<TOther>.Error(commandResult);
    }
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
    public static CommandResult<T> Ok<T>(T value) => CommandResult<T>.Ok(value);

    public static CommandResult BadRequest(object error) => new(StatusCodes.Status400BadRequest, error);
    public static CommandResult NotFound(object error) => new(StatusCodes.Status404NotFound, error);

    public static CommandResult Unauthorized() => new(StatusCodes.Status401Unauthorized, null);
    public static CommandResult Unauthorized(object error) => new(StatusCodes.Status401Unauthorized, error);
    public static CommandResult Forbidden() => new(StatusCodes.Status403Forbidden, null);
    public static CommandResult Forbidden(object error) => new(StatusCodes.Status403Forbidden, error);

    internal static CommandResult Error(ICommandResult other)
    {
        if (!other.IsError()) throw new Exception("Successful result used as error");
        return new CommandResult(other.StatusCode, other.Content);
    }
}

public class CommandResult<T> : ICommandResult
{
    public int StatusCode { get; }
    public T? Value { get; }
    public object? Content { get; }

    private CommandResult(int statusCode, T? value, object? content)
    {
        StatusCode = statusCode;
        Content = content;
        Value = value;
    }

    internal static CommandResult<T> Ok(T value) => new(StatusCodes.Status200OK, value, value);

    public static CommandResult<T> Error(ICommandResult other)
    {
        if (!other.IsError()) throw new Exception("Successful result used as error");
        return new CommandResult<T>(other.StatusCode, default, other.Content);
    }
}
