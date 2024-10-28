using Microsoft.AspNetCore.Mvc;

using System;
using System.Net;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public class InvalidStatusCodeException(string message) : Exception(message) { }

/// <summary>The result from a command, defining the HTTP response</summary>
public interface ICommandResult
{
    /// <summary>The status code for the HTTP response</summary>
    public HttpStatusCode StatusCode { get; }
    /// <summary>The body content for the HTTP response</summary>
    public object? Content { get; }
}

/// <summary>The result from a command, defining the HTTP response</summary>
public sealed class CommandResult : ICommandResult
{
    public HttpStatusCode StatusCode { get; }
    public object? Content { get; }

    private CommandResult(HttpStatusCode statusCode, object? content)
    {
        StatusCode = statusCode;
        Content = content is Exception exception
            ? new { error = exception.Message }
            : content;
    }

    /// <summary>204 NO CONTENT</summary>
    public static CommandResult NoContent() => new(HttpStatusCode.NoContent, null);
    /// <summary>204 NO CONTENT</summary>
    public static CommandResult<T> NoContent<T>() where T : class => CommandResult<T>.NoContent();
    /// <summary>200 OK</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The response DTO</param>
    public static CommandResult<T> Ok<T>(T dto) where T : class => CommandResult<T>.Ok(dto);

    /// <summary>400 BAD REQUEST</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError BadRequest(object error) => CommandError.BadRequest(error);
    /// <summary>409 CONFLICT</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Conflict(object error) => CommandError.Conflict(error);
    /// <summary>404 NOT FOUND</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError NotFound(object error) => CommandError.NotFound(error);

    /// <summary>401 UNAUTHORIZED</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Unauthorized() => CommandError.Unauthorized();
    /// <summary>401 UNAUTHORIZED</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Unauthorized(object error) => CommandError.Unauthorized(error);
    /// <summary>403 FORBIDDEN</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Forbidden() => CommandError.Forbidden();
    /// <summary>403 FORBIDDEN</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Forbidden(object error) => CommandError.Forbidden(error);

    /// <summary>Construct an error result</summary>
    /// <param name="statusCode">A 4xx or 5xx status code</param>
    /// <param name="content">The body content for the HTTP response</param>
    public static CommandError Error(HttpStatusCode statusCode, object? content = null) => new(statusCode, content);

    public static implicit operator CommandResult(CommandError errorResult) =>
        new(errorResult.StatusCode, errorResult.Content);
}

/// <summary>A command result with an expected value (DTO) to send in the HTTP response body</summary>
/// <typeparam name="T">The type of the DTO to send if the result is successful</typeparam>
public sealed class CommandResult<T> : ICommandResult where T : class
{
    public HttpStatusCode StatusCode { get; }
    public object? Content { get; }

    /// <summary>The value (DTO) to send in the response (null if error)</summary>
    public T? Value { get; }

    /// <summary>200 OK</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value (DTO) to send in the response</param>
    public static CommandResult<T> Ok(T value) => new(HttpStatusCode.OK, value, value);
    /// <summary>204 NO CONTENT</summary>
    public static CommandResult<T> NoContent() => new(HttpStatusCode.NoContent, null, null);

    private CommandResult(HttpStatusCode statusCode, T? value, object? content)
    {
        Value = value;
        StatusCode = statusCode;
        Content = content;
    }

    public static implicit operator CommandResult<T>(CommandError errorResult) =>
      new(errorResult.StatusCode, null, errorResult.Content);
}

/// <summary>A command result with an error status</summary>
/// <param name="statusCode">The error status (4xx or 5xx) for the HTTP response</param>
/// <param name="content">The body content for the HTTP response</param>
public sealed class CommandError(HttpStatusCode statusCode, object? content) : ICommandResult
{
    public HttpStatusCode StatusCode { get; } = GuardIsError(statusCode);
    public object? Content { get; } = content;

    /// <summary>400 BAD REQUEST</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError BadRequest(object error) => new(HttpStatusCode.BadRequest, error);
    /// <summary>409 CONFLICT</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Conflict(object error) => new(HttpStatusCode.Conflict, error);
    /// <summary>404 NOT FOUND</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError NotFound(object error) => new(HttpStatusCode.NotFound, error);

    /// <summary>401 UNAUTHORIZED</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Unauthorized() => new(HttpStatusCode.Unauthorized, null);
    /// <summary>401 UNAUTHORIZED</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Unauthorized(object error) => new(HttpStatusCode.Unauthorized, error);
    /// <summary>403 FORBIDDEN</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Forbidden() => new(HttpStatusCode.Forbidden, null);
    /// <summary>403 FORBIDDEN</summary>
    /// <param name="error">The body content for the HTTP response</param>
    public static CommandError Forbidden(object error) => new(HttpStatusCode.Forbidden, error);

    private static HttpStatusCode GuardIsError(HttpStatusCode statusCode)
    {
        if ((int)statusCode < 400)
            throw new InvalidStatusCodeException($"Status code {statusCode} is not an error code");
        return statusCode;
    }
}

public static class CommandResultExtension
{
    public static ActionResult ActionResult(this ICommandResult result) =>
        result.Content is null
            ? new StatusCodeResult((int)result.StatusCode)
            : new OkObjectResult(result.Content) { StatusCode = (int)result.StatusCode };

    public static bool IsError(this ICommandResult commandResult) => (int)commandResult.StatusCode is < 200 or >= 300;

    /// <summary>Construct a <see cref="CommandError"/> result value that can be implicitly converted to any <see cref="ICommandResult"/> type</summary>
    /// <param name="original">The original result (that is expected to be an error</param>
    /// <returns>a copy of the original result if it is an error, otherwize null</returns>
    public static CommandError? AsError(this ICommandResult original) => IsError(original) ? SameError(original) : null;

    /// <summary>Construct a <see cref="CommandError"/> result value that can be implicitly converted to any <see cref="ICommandResult"/> type</summary>
    /// <param name="commandResult">The original result (that is expected to be an error</param>
    /// <returns></returns>
    public static CommandError SameError(this ICommandResult commandResult) =>
        new(commandResult.StatusCode, commandResult.Content);
}
