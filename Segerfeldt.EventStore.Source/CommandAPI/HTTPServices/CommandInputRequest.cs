using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Segerfeldt.EventStore.Shared;

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
        var command = await new CommandParser(context)
            .GetCommandDTOAsync(handlerType.GetMethod(nameof(ICommandHandler<object>.Handle))!);

        var commandHandlerExecuter = new CommandHandlerExecuter(serviceLocator.CreateInstance(handlerType));
        var commandContext = new CommandContext
        {
            EventPublisher = serviceLocator.GetServiceOrCreateInstance<EventPublisher>(),
            EntityStore = serviceLocator.GetServiceOrCreateInstance<EntityStore>(),
            Request = new WrappedHttpContext(context),
            HttpContext = context
        };
        var (actionResult, dto) = await commandHandlerExecuter.ExecuteHandlerAsync(command, commandContext);

        return dto is null ? actionResult : new OkObjectResult(dto);
    }

    private record TaskResult(ActionResult actionResult, object? dto = null);

    private class BadRequestException : Exception
    {
        public object? ErrorData { get; }

        public BadRequestException(string message, object? errorData = null) : base(message)
        {
            ErrorData = errorData;
        }
    }

    private class CommandParser
    {
        private readonly HttpContext context;

        public CommandParser(HttpContext context)
        {
            this.context = context;
        }

        public async Task<object> GetCommandDTOAsync(MethodBase handleMethod)
        {
            var handleMethodParameters = handleMethod.GetParameters();
            if (handleMethodParameters is { Length: < 1 or > 2 }) throw new BadRequestException("");

            var command = await DeserializeCommand(handleMethodParameters[0].ParameterType);
            if (command is null) throw new BadRequestException("Command is null");

            var missingProperties = GetMissingProperties(command).ToList();
            if (missingProperties.Any())
                throw new BadRequestException(
                    "Not all required properties are specified",
                    new
                    {
                        error = "Not all required properties are specified",
                        missingProperties
                    });

            var invalidProperties = GetInvalidProperties(command).ToList();
            if (invalidProperties.Any())
                throw new BadRequestException(
                    "Not all properties are valid",
                    new
                    {
                        error = "Not all properties are valid",
                        invalid = new Dictionary<string, string?>(invalidProperties)
                    });

            return command;
        }

        private async Task<object?> DeserializeCommand(Type commandType) =>
            context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Delete
                ? DeserializeQueryCommand(commandType)
                : await DeserializeJSONCommand(commandType);

        private object? DeserializeQueryCommand(Type commandType)
        {
            var command = commandType.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
            foreach (var (key, value) in context.Request.Query)
                commandType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?
                    .SetValue(command, value.FirstOrDefault());
            return command;
        }

        private async Task<object?> DeserializeJSONCommand(Type commandType) =>
            await JSON.DeserializeAsync(context.Request.Body, commandType);

        private static IEnumerable<KeyValuePair<string, string?>> GetInvalidProperties(object command) =>
            command.GetType().GetProperties()
                .SelectMany(p => p.GetCustomAttributes<ValidationAttribute>()
                    .Select(attribute =>
                        attribute.GetValidationResult(p.GetValue(command), new ValidationContext(command) {DisplayName = p.Name}))
                    .Where(r => r is not null).Select(r => r!)
                    .Select(r => new KeyValuePair<string, string?>(p.Name, r.ErrorMessage)));

        private static IEnumerable<string> GetMissingProperties(object command) =>
            command.GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<RequiredAttribute>() is not null)
                .Where(p => p.GetValue(command) is null)
                .Select(p => p.Name);

    }

    private class CommandHandlerExecuter
    {
        private readonly object handler;

        public CommandHandlerExecuter(object handler)
        {
            this.handler = handler;
        }

        public async Task<TaskResult> ExecuteHandlerAsync(object? command, CommandContext context)
        {
            Task task;
            try
            {
                task = InvokeHandler(handler, command, context);
                await task;
            }
            catch (Exception e)
            {
                var errorResult = new ObjectResult(new { error = e.Message }) { StatusCode = StatusCodes.Status500InternalServerError };
                return new(errorResult);
            }
            var handlerResult = (ICommandResult)task.GetPropertyValue(nameof(Task<object>.Result))!;
            var actionResult = handlerResult.ActionResult();
            var value = handlerResult.GetPropertyValue(nameof(ActionResult<object>.Value));
            return new(actionResult, value);
        }

        private static Task InvokeHandler(object handler, object? command, CommandContext context)
        {
            var parameters = command is null ? new[] { context } : new[] { command, context };
            return (Task)handler.InvokeMethod(nameof(ICommandHandler<object>.Handle), parameters)!;
        }
    }
}

internal static class ObjectExtensions
{
    public static object? GetPropertyValue(this object o, string propertyName) =>
        o.GetType().GetProperty(propertyName)?.GetValue(o);

    public static object? InvokeMethod(this object o, string methodName, object[] parameters) =>
        o.GetType().GetMethod(methodName)?.Invoke(o, parameters);
}
