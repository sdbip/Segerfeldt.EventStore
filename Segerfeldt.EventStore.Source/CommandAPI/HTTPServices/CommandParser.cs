using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class CommandParser(HttpContext context)
{
    private readonly HttpContext context = context;

    public async Task<object> GetCommandDTOAsync(MethodBase handleMethod)
    {
        var handleMethodParameters = handleMethod.GetParameters();
        var command = await DeserializeCommand(handleMethodParameters[0].ParameterType)
            ?? throw new ParseException("Command is null");
        var missingProperties = GetMissingProperties(command).ToList();
        if (missingProperties.Any())
            throw new ParseException(
                "Not all required properties are specified",
                new
                {
                    error = "Not all required properties are specified",
                    missingProperties
                });

        var invalidProperties = GetInvalidProperties(command).ToList();
        if (invalidProperties.Any())
            throw new ParseException(
                "Not all properties are valid",
                new
                {
                    error = "Not all properties are valid",
                    invalid = new Dictionary<string, string?>(invalidProperties)
                });

        return command;
    }

    private async Task<object> DeserializeCommand(Type commandType) =>
        context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Delete
            ? DeserializeQueryCommand(commandType)
            : await DeserializeJSONCommand(commandType);

    private object DeserializeQueryCommand(Type commandType)
    {
        var command = commandType.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>())
                ?? throw new Exception($"The type {commandType.Name} cannot be instantiated from an empty constructor.");

        foreach (var (key, value) in context.Request.Query)
            commandType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?
                .SetValue(command, value.FirstOrDefault());
        return command;
    }

    private async Task<object> DeserializeJSONCommand(Type commandType) =>
        await JSON.DeserializeAsync(context.Request.Body, commandType)
            ?? throw new ParseException($"Unable to parse body as {commandType.Name}");

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
