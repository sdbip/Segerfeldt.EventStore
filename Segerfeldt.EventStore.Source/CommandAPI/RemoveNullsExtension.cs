using System.Collections.Generic;
using System.Linq;

namespace Segerfeldt.EventStore.Source.CommandAPI;

internal static class RemoveNullsExtension
{
    public static IEnumerable<T> RemoveNulls<T>(this IEnumerable<T?> list) where T : struct =>
        list.Where(item => item is not null).Select(item => item!.Value);
}
