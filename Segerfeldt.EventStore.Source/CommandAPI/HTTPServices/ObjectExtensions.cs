namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal static class ObjectExtensions
{
    public static object? GetPropertyValue(this object o, string propertyName) =>
        o.GetType().GetProperty(propertyName)?.GetValue(o);

    public static object? InvokeMethod(this object o, string methodName, params object[] parameters) =>
        o.GetType().GetMethod(methodName)?.Invoke(o, parameters);
}
