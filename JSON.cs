using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore
{
    internal static class JSON
    {
        private static JsonSerializerOptions Options => new() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

        public static string Serialize(object o) => JsonSerializer.Serialize(o, Options);

        public static T? Deserialize<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString, Options);
        internal static object? Deserialize(string jsonString, Type type) => JsonSerializer.Deserialize(jsonString, type, Options);

        internal static async Task SerializeAsync(Stream stream, object o) => await JsonSerializer.SerializeAsync(stream, o, Options);

        internal static async Task<T?> DeserializeAsync<T>(Stream stream) => await JsonSerializer.DeserializeAsync<T>(stream, Options);
        internal  static async Task<object?> DeserializeAsync(Stream stream, Type type) => await JsonSerializer.DeserializeAsync(stream, type, Options);
    }
}
