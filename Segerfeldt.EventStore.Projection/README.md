<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Segerfeldt.EventStore.Projection

A NuGet package for implementing the Query/Read-Model side of a CQRS application with your custom database.

Add Segerfeldt.EventStore.Source to another project to generate the events on the Command/Write-Model side.

# Setup

You will need to set up a database connection for each write-model database (a.k.a. `EventSource`) you want to project state from. If you don't want to define your own provider there are three packages too choose from for built-in database support.

- Segerfeldt.EventStore.Projection.PostgreSQL
- Segerfeldt.EventStore.Projection.MSSQL
- Segerfeldt.EventStore.Projection.SQLite

If you add either of the above packages Segerfeldt.EventStore.Projection will be added implicitly.

Call the extension method `IServiceCollection.AddHostedEventSource(IEventSourceProvider)` to subscribe to a write-model using your custom provider:

```c#
builder.Services.AddKeyedSingleton("source-1", new MyCustomDbConnection(builder.Configuration.GetConnectionString("source_database")));
builder.Services.AddHostedEventSource<MyCustomEventSourceRepository>("source-1", new EventSourceOptions
{
    Initialization = (IServiceProvider provider) =>
    {
        // Perform initialization as needed. A typical task might be to update the schema of the target database.
        // Use the provider locate necessary services.
    }
})
    .SetTarget(new MyCustomDbConnection(builder.Configuration.GetConnectionString("target_database")!))
    .AddReceptacles()
    .SetProjectionTracker<MyCustomProjectionTracker>();
```

You can add multiple sources (and they don't all have to use the same provider). Just make sure that they are logically separated in the projection database (or use transactions) as they will emit events on independent threads:

```c#
builder.Services.AddHostedPostgreSQLEventSource("source-1", builder.Configuration.GetConnectionString("source1_database")!)
    .SetPostgreSQLTarget(builder.Configuration.GetConnectionString("target_database")!)
    .AddReceptacles()
    .SetProjectionTracker<Source1ProjectionTracker>();

builder.Services.AddHostedSQLServerEventSource("source-2", builder.Configuration.GetConnectionString("source2_database")!)
    .SetSQLServerTarget(builder.Configuration.GetConnectionString("target_database")!)
    .AddReceptacles()
    .SetProjectionTracker<Source2ProjectionTracker>();

builder.Services.AddHostedSQLiteEventSource("source-3", builder.Configuration.GetConnectionString("source3_database")!)
    .SetSQLiteTarget(builder.Configuration.GetConnectionString("target_database")!)
    .AddReceptacles()
    .SetProjectionTracker<Source3ProjectionTracker>();
```

# Implementation

Receptacles are detected automatically in the specified assemblies. All `public` classes that implement `IReceptacle` will be notified.

```c#
using Segerfeldt.EventStore.Projection;

namespace ProjectionApp;

public record IncrementDTO(int amount);

// The abstract class ReceptacleBase is a useful shortcut to implementing IReceptacle.
// It is not necessary to inherit from that class, but it is necessary to implement IReceptacle.
public sealed class CounterState : ReceptacleBase
{
    // This will be called for every "Registered" event where the Entity.Type is "Counter".
    // The EntityType property is optional. If the same event name is used for multiple
    // entities, this property discards irrelevant events.
    [ReceivesEvent("Registered", EntityType = "Counter")]
    public void ReceiveNewCounter(string entityId, EmptyDetails details)
    {
        // This should usually update a projection database.
        InsertCounterRow(entityId);
    }

    // This will be called for every "Incremented" event regardless of entity type.
    [ReceivesEvent("Incremented")]
    public void ReceiveCounterIncrement(string entityId, IncrementDTO details)
    {
        // This should usually update a projection database.
        IncrementAmountForCounterRow(entityId, details.Amount);
    }
}
```

The `ProjectionTracker` is used to maintain a persisted memory of your place in the event stream. When the synchronization
service is restarted it should not restart syncing from the first event. The `ProjectionTracker` will be notified as the
position changes so that it can update the persisted value.

```c#
using Segerfeldt.EventStore.Projection;

namespace ProjectionApp;

public sealed class ProjectionTracker : IProjectionTracker
{
    public long? GetLastFinishedPosition()
    {
        // This is called at launch (or shortly thereafter)
        // to determine which events to skip in the first update.
        return GetPersistedPosition();
    }

    public void OnProjectionStarting(long position)
    {
        // This is called before emitting any events at this position.

        // This might be a good place to call BEGIN TRANSACTION.

        // If an exception is thrown during projection, some receptacles might alredy have been updated.
        // It is good to be able to undo (ROLLBACK) those changes in that case.
    }

    public void OnProjectionFinished(long position)
    {
        // This is called after notifying receptacles.
        PersistPosition(position);

        // This might be a good place to COMMIT the changes
    }

    public void OnProjectionError(long position)
    {
        // This might be a good place to ROLLBACK the changes

        // The next time the Projection system emits events it will replay from this failed position.
        // If some of the receptacles did successfully receive their updates before the error, they will be
        // doubly notified, which might result in corruption.
    }
}
```
