using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public interface ICommandResult
{
    int StatusCode { get; }
    object? Message { get; }
}

internal static class CommandResultStatus
{
    public static ActionResult ActionResult(this ICommandResult handlerResult) =>
        handlerResult.Message is null
            ? new StatusCodeResult(handlerResult.StatusCode)
            : new ObjectResult(handlerResult.Message) { StatusCode = handlerResult.StatusCode };
}

public struct ResultData
{
    public int StatusCode { get; }
    public object? Message { get; }

    public ResultData(int statusCode, object? message)
    {
        Message = message;
        StatusCode = statusCode;
    }
}

public class CommandResult : ICommandResult
{
    private readonly ResultData data;

    public int StatusCode { get => data.StatusCode; }
    public object? Message { get => data.Message; }

    private CommandResult(ResultData data) => this.data = data;

    public static CommandResult Ok() => new(new ResultData(StatusCodes.Status204NoContent, null));
    public static CommandResult<T> Ok<T>(T value) => new(new ResultData(StatusCodes.Status200OK, value));

    public static ResultData BadRequest(object error) => Error(StatusCodes.Status400BadRequest, error);
    public static ResultData NotFound(object error) => Error(StatusCodes.Status404NotFound, error);

    public static ResultData Unauthorized() => Error(StatusCodes.Status401Unauthorized);
    public static ResultData Unauthorized(object error) => Error(StatusCodes.Status401Unauthorized, error);
    public static ResultData Forbidden() => Error(StatusCodes.Status403Forbidden);
    public static ResultData Forbidden(object error) => Error(StatusCodes.Status403Forbidden, error);

    public static ResultData Error(int statusCode, object? error = null) => new(statusCode, error);
    public static implicit operator CommandResult(ResultData data) => new(data);
}

public class CommandResult<T> : ICommandResult
{
    private readonly ResultData data;

    public int StatusCode { get => data.StatusCode; }
    public object? Message { get => data.Message; }

    public CommandResult(ResultData data) => this.data = data;

    public static implicit operator CommandResult<T>(ResultData data) => new(data);
}
