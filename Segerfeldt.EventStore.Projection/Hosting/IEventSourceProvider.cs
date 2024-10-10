using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Projection.Hosting;

/// <summary>A provider that knows how to set up dtabase connections</summary>
public interface IEventSourceProvider
{
    /// <summary>Called at startup to prepare the write-model database</summary>
    /// This can be used to execute schema DDLs for your position tracker.
    /// <param name="serviceProvider">the Web API service provider</param>
    void PrepareDatabase(IServiceProvider serviceProvider);

    /// <summary>Called to create new connections to the database</summary>
    /// <returns>a connection to the write-model database</returns>
    DbConnection CreateConnection();
}
