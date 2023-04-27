using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class CommandInputRequest
{
    private readonly HttpContext context;
    private readonly Type handlerType;

    public CommandInputRequest(Type handlerType, HttpContext context)
    {
        this.context = context;
        this.handlerType = handlerType;
    }

    public async Task<ActionResult> Execute()
    {
        var serviceLocator = new ServiceLocator(context.RequestServices);
        var handler = serviceLocator.CreateInstance(handlerType);
        var method = handler.GetType().GetMethod(nameof(ICommandHandler<int>.Handle))!;

        var commandResult = await ParseCommand(method);
        if (commandResult.Value is null) return commandResult.Result!;

        var commandContext = new CommandContext
        {
            EventPublisher = serviceLocator.GetServiceOrCreateInstance<EventPublisher>(),
            EntityStore = serviceLocator.GetServiceOrCreateInstance<EntityStore>(),
            HttpContext = context
        };
        var (actionResult, dto) = await ExecuteHandlerAsync(handler, method, commandResult.Value, commandContext);

        return dto is null ? actionResult : new OkObjectResult(dto);
    }

    private async Task<ActionResult<object>> ParseCommand(MethodBase method)
    {
        var commandType = method.GetParameters()[0].ParameterType;
        object? command;
        if (context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Delete)
            command = DeserializeQueryCommand(commandType);
        else
            command = await DeserializeCommand(commandType);
        if (command is null) return new ActionResult<object>(new BadRequestObjectResult("Command is null"));

        var missingProperties = GetMissingProperties(command).ToList();
        if (missingProperties.Any())
            return new BadRequestObjectResult(new { error = "Not all required properties are specified", missing = missingProperties });

        var invalidProperties = GetInvalidProperties(command).ToList();
        if (invalidProperties.Any())
            return new BadRequestObjectResult(new { error = "Not all properties are valid", invalid =
                new Dictionary<string, string?>(invalidProperties) });

        return command;
    }

    private object? DeserializeQueryCommand(Type commandType)
    {
        var command = commandType.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
        foreach (var (key, value) in context.Request.Query)
            commandType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?.SetValue(command, value.FirstOrDefault());
        return command;
    }

    private async Task<object?> DeserializeCommand(Type commandType) =>
        await JSON.DeserializeAsync(context.Request.Body, commandType);

    private static IEnumerable<KeyValuePair<string, string?>> GetInvalidProperties(object command) =>
        command
            .GetType()
            .GetProperties()
            .SelectMany(p => p.GetCustomAttributes<ValidationAttribute>()
                .Select(attribute =>
                    attribute.GetValidationResult(p.GetValue(command), new ValidationContext(command) {DisplayName = p.Name}))
                .Where(r => r is not null).Select(r => r!)
                .Select(r => new KeyValuePair<string, string?>(p.Name, r.ErrorMessage)));

    private static IEnumerable<string> GetMissingProperties(object command) =>
        command
            .GetType()
            .GetProperties()
            .Where(p => p.GetCustomAttribute<RequiredAttribute>() is not null)
            .Where(p => p.GetValue(command) is null)
            .Select(p => p.Name);

    private static async Task<(ActionResult actionResult, object? dto)> ExecuteHandlerAsync(object? handler, MethodBase method, object? command, CommandContext context)
    {
        Task task;
        try
        {
            task = (Task)method.Invoke(handler, new[] { command, context })!;
            await task;
        }
        catch (Exception e)
        {
            var errorResult = new ObjectResult(new { error = e.Message }) { StatusCode = StatusCodes.Status500InternalServerError };
            return (errorResult, dto: null);
        }
        var handlerResult = (ICommandResult)task.GetType().GetProperty(nameof(Task<int>.Result))!.GetValue(task)!;
        var actionResult = handlerResult.ActionResult();
        var value = handlerResult.GetType().GetProperty(nameof(ActionResult<int>.Value))?.GetValue(handlerResult);
        return (actionResult, dto: value);
    }
}
