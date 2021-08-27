using System;
using System.Text.Json;

namespace Segerfeldt.EventStore
{
    internal static class JSON
    {
        private static JsonSerializerOptions Options => new() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

        public static string Serialize(object o) => JsonSerializer.Serialize(o, Options);

        public static T? Deserialize<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString, Options);
        internal static object? Deserialize(string jsonString, Type type) => JsonSerializer.Deserialize(jsonString, type, Options);
    }
}
