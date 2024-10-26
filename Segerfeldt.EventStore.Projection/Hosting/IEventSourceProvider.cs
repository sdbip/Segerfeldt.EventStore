using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.Hosting;

/// <summary>A provider that knows how to set up dtabase connections</summary>
public interface IEventSourceProvider
{
    /// <summary>Called at startup to prepare the projection database for receiving events from this provider</summary>
    /// This can be used to execute schema DDLs for your position tracker.
    /// <param name="serviceProvider">the Web API service provider</param>
    void PrepareToReceive(IServiceProvider serviceProvider);

    /// <summary>Called to create new connections to the database</summary>
    /// <returns>a connection to the write-model database</returns>
    IDbConnection CreateConnection(IServiceProvider serviceProvider);
}
