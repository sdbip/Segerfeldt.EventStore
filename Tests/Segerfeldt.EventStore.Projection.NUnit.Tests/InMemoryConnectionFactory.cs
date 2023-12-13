using Microsoft.Data.Sqlite;

using System.Data;

namespace Segerfeldt.EventStore.Projection.Tests;

internal static class InMemoryConnectionFactory
{
  public static IDbConnection OpenNew()
  {
      var connection = new SqliteConnection("Data Source = :memory:");
      connection.Open();
      return connection;
  }
}
