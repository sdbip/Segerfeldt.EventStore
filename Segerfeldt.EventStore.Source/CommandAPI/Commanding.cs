using JetBrains.Annotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    [PublicAPI]
    public static class Commanding
    {
        public static void DocumentCommands(this SwaggerGenOptions swaggerOptions, params Assembly[] assemblies)
        {
            swaggerOptions.DocumentFilter<HistoryDocumentFilter>();
            swaggerOptions.DocumentFilter<CommandsDocumentFilter>(assemblies.AsEnumerable());
        }

        public static void MapCommands(this IEndpointRouteBuilder endpoints, params Assembly[] assemblies)
        {
            endpoints.MapGet("history/{entityId}", GetHistory);

            var handlerTypes = assemblies
                .SelectMany(assembly => assembly
                    .GetExportedTypes()
                    .Where(type => type.IsClass)
                    .Where(type => !type.IsAbstract));

            foreach (var handlerType in handlerTypes)
            {
                if (handlerType.GetCustomAttribute<ModifiesEntityAttribute>(false) is not { } attribute) continue;
                var pattern = attribute.Pattern;
                endpoints.MapMethods(pattern, new []{attribute.Method.ToString()}, async context => { await HandleCommand(handlerType, context); });
            }
        }

        private static async Task GetHistory(HttpContext context)
        {
            var id = (string?)context.GetRouteValue("entityId");
            var store = ActivatorUtilities.GetServiceOrCreateInstance<EntityStore>(context.RequestServices);

            var history = await store.GetHistoryAsync(new EntityId(id!));
            if (history is null)
                await ApplyResult(new NotFoundObjectResult($"There is no entity with the id '{id}'"), context);
            else
                await ApplyResult(new OkObjectResult(History.From(history)), context);
        }

        private static async Task HandleCommand(Type handlerType, HttpContext context)
        {
            var handler = ActivatorUtilities.CreateInstance(context.RequestServices, handlerType);
            var method = handler.GetType().GetMethod(nameof(ICommandHandler<int>.Handle))!;

            var commandResult = await ParseCommand(method, context);
            if (commandResult.Value is null)
            {
                await ApplyResult(commandResult.Result, context);
                return;
            }

            var commandContext = new CommandContext(
                ActivatorUtilities.GetServiceOrCreateInstance<EventPublisher>(context.RequestServices),
                ActivatorUtilities.GetServiceOrCreateInstance<EntityStore>(context.RequestServices),
                context);
            var (actionResult, dto) = await ExecuteHandlerAsync(handler, method, commandResult.Value, commandContext);

            var result = dto is null ? actionResult : new OkObjectResult(dto);
            await ApplyResult(result, context);
        }

        private static async Task<ActionResult<object>> ParseCommand(MethodBase method, HttpContext context)
        {
            var commandType = method.GetParameters()[0].ParameterType;
            object? command;
            if (context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Delete)
                command = DeserializeQueryCommand(commandType, context);
            else
                command = await DeserializeCommand(commandType, context);
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

        private static object? DeserializeQueryCommand(Type commandType, HttpContext context)
        {
            var command = commandType.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
            foreach (var (key, value) in context.Request.Query)
                commandType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?.SetValue(command, value.FirstOrDefault());
            return command;
        }

        private static async Task<object?> DeserializeCommand(Type commandType, HttpContext context) =>
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

        private static async Task ApplyResult(IActionResult actionResult, HttpContext context)
        {
            await actionResult.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
