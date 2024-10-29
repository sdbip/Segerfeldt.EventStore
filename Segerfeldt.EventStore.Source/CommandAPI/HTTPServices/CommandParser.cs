using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal sealed class CommandParser(HttpContext context)
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    private readonly HttpContext context = context;

    public async Task<object> GetCommandDTOAsync(MethodBase handleMethod, ModifiesEntityAttribute attribute)
    {
        var handleMethodParameters = handleMethod.GetParameters();
        var command = await DeserializeCommand(handleMethodParameters[0].ParameterType, attribute)
            ?? throw new ParseException("Command is null");
        var missingProperties = GetMissingProperties(command);
        if (missingProperties.Any())
            throw new ParseException(
                "Not all required properties are specified",
                new
                {
                    error = "Not all required properties are specified",
                    missingProperties
                });

        var invalidProperties = GetInvalidProperties(command);
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

    private async Task<object> DeserializeCommand(Type commandType, ModifiesEntityAttribute attribute) =>
        attribute.SerializationType == CommandSerializationMode.URLQuery
            ? DeserializeQueryCommand(commandType)
            : await DeserializeJSONCommand(commandType);

    private object DeserializeQueryCommand(Type commandType)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (key, value) in context.Request.Query) dict.Add(key, (string)value!);
        var json = JSON.Serialize(dict);

        return JSON.Deserialize(json, commandType)
            ?? throw new Exception($"The type {commandType.Name} cannot be instantiated from an empty constructor.");
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
            .Where(p => p.GetCustomAttribute<RequiredAttribute>() is not null || NullabilityContext.Create(p).WriteState == NullabilityState.NotNull)
            .Where(p => p.GetValue(command) is null)
            .Select(p => p.Name);

}
