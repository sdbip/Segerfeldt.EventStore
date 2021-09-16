using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    public static class Commanding
    {
        public static void DocumentCommands(this SwaggerGenOptions swaggerOptions, params Assembly[] assemblies)
        {
            swaggerOptions.DocumentFilter<HistoryDocumentFilter>();
            swaggerOptions.DocumentFilter<CommandsDocumentFilter>(assemblies.AsEnumerable());
        }

        public static void MapCommands(this IEndpointRouteBuilder endpoints, params Assembly[] assemblies)
        {
            endpoints.MapGet("history/{id}", GetHistory);

            var handlerTypes = assemblies
                .SelectMany(assembly => assembly
                    .GetExportedTypes()
                    .Where(type => type.IsClass)
                    .Where(type => !type.IsAbstract));

            foreach (var handlerType in handlerTypes)
            {
                if (handlerType.GetCustomAttribute<HandlesCommandAttribute>(false) is not { } attribute) continue;
                var pattern = attribute.Pattern;
                endpoints.MapPost(pattern, async context => { await HandleCommand(handlerType, context); });
            }
        }

        private static async Task GetHistory(HttpContext context)
        {
            var id = (string?)context.GetRouteValue("id");
            var store = context.RequestServices.GetRequiredService<EntityStore>();

            var history = await store.GetHistoryAsync(new EntityId(id!));
            if (history is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync($"There is no entity with the id '{id}'");
            }
            else
            {
                var dto = History.From(history);
                context.Response.StatusCode = StatusCodes.Status200OK;
                await JSON.SerializeAsync(context.Response.Body, dto);
            }
        }

        private static async Task HandleCommand(Type handlerType, HttpContext context)
        {
            var handler = ActivatorUtilities.CreateInstance(context.RequestServices, handlerType);
            var method = handler.GetType().GetMethod(nameof(ICommandHandler<int>.Handle))!;

            var command = await DeserializeCommand(method, context);

            var actionResult = await ExecuteHandlerAsync(handler, method, command, context);
            if (!actionResult.GetType().IsGenericType)
            {
                await ApplyResult(actionResult, context);
                return;
            }

            var dto = actionResult.GetType().IsGenericType
                ? actionResult.GetType().GetProperty(nameof(ActionResult<int>.Value))?.GetValue(actionResult)
                : null;
            if (dto is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.Body.Close();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            await JSON.SerializeAsync(context.Response.Body, dto);
        }

        private static async Task<object?> DeserializeCommand(MethodBase method, HttpContext context)
        {
            var commandType = method.GetParameters()[0].ParameterType;
            return await JSON.DeserializeAsync(context.Request.Body, commandType);
        }

        private static async Task<object> ExecuteHandlerAsync(object? handler, MethodBase method, object? command, HttpContext context)
        {
            var task = (Task)method.Invoke(handler, new[] { command, context })!;
            await task;
            return task.GetType().GetProperty(nameof(Task<int>.Result))!.GetValue(task)!;
        }

        private static async Task ApplyResult(object actionResult, HttpContext context)
        {
            var result = actionResult.GetType().IsGenericType
                ? (ActionResult)actionResult.GetType().GetProperty(nameof(ActionResult<int>.Result))!.GetValue(actionResult)!
                : (ActionResult)actionResult;
            await result.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
