using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Segerfeldt.EventStore.Projection;

internal static class DatabaseExtensions
{
    public static T OpenAndExecute<T>(this IDbConnection connection, Func<IDbConnection, T> action)
    {
        connection.Open();
        try { return action(connection); }
        finally { connection.Close(); }
    }

    public static IEnumerable<T> AllRowsAs<T>(this IDataReader reader, Func<IDataReader, T> readItem) =>
       ReadRows(reader, readItem).ToList();

    private static IEnumerable<T> ReadRows<T>(IDataReader reader, Func<IDataReader, T> readItem)
    {
        while (reader.Read())
            yield return readItem(reader);
    }
}
