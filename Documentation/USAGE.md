# Segerfeldt.EventSourcing Usage

This document is meant to help develop clients using the Segerfeldt.EventSourcing NuGet packages. If you are not familiar with thew concepts, there is documentation describing [event-sourcing](./ES.md) you should probably read first.

See the Apps/ directory for example applications.

- ProjectionWebApplication: an example of subscribing to published events in order to track aggregate state for multiple entities
- SourceConsoleApp: an example of direct (simpler) generation and publishing of events
- SourceWebApplication: a complete (more complex) example of publishing of events through command handlers in a web application

## Source Setup

The Source library is meant to implement the Command side (a.k.a. the write model) of a CQRS system. Import this in your web service code to start manipulating entities and publishing events.

Add the following line to your Program.cs to automatically find and map endpoints for the command handlers you have defined in your main assembly.

```csharp
app.MapCommands(Assembly.GetExecutingAssembly());
```

You can optionally define your endpoints in a different assembly (or in several). Just make sure to pass them as arguments to the `MapCommands` call:

```csharp
app.MapCommands(assembly1, assembly2);
```

Add the following code to your services setup if you want Swagger documentation of your commands:

```csharp
services.AddSwaggerGen(options =>
{
    options.DocumentCommands(Assembly.GetExecutingAssembly());
    options.IncludeXmlComments(Path.Combine(
        AppContext.BaseDirectory,
        "<application name>.xml"));
});
```

The `IncludeXmlComments` call is optional. If you do use it, you will need to also turn on XML documentation in your .csproj file:

```xml
<PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <DocumentationFile>bin\Release\net7.0\[application name].xml</DocumentationFile>
</PropertyGroup>
```

See the [SourceWebApplication](../Apps/SourceWebApplication/Program.cs) for a functioning example.

## Source Implementation

Define a command-handler by adding a class like this:

```c#
using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using static Segerfeldt.EventStore.Source.CommandAPI.CommandResult;

// It is recommended to separate commands and entities in different namespaces
// (or even different assemblies).
using Domain;
namespace Commands;

public record IncrementCounter(int amount);

// Implement the ICommandHandler<> interface to declare a command handler. The
// `ModifiesEntityAttribute` (or one of its companions) defines the signature
// for a RESTful endpoint that executes the command.
[ModifiesEntity("Counter")]
public sealed class IncrementCounterCommandHandler : ICommandHandler<IncrementCounter>
{
    public async Task<CommandResult> Handle(IncrementCounter command, CommandContext context)
    {
        // The actor is the user that executes the command.
        // The name of the current principal is usually a good choice.
        var actor = context.HttpContext.User?.Name;

        // Unauthorized() returns status 401 UNAUTHORIZED, which indicates failed authentication.
        // Forbidden() returns status 403 FORBIDDEN which indicates that the user is not authorized.
        // See https://www.webfx.com/web-development/glossary/http-status-codes/ for details.
        if (actor is null) return Unauthorized();
        if (!IsAuthorized(actor)) return Forbidden();

        // The path of the request contains the id when modifying an existing entity.
        var id = new EntityId(context.GetRouteParameter("entityid"));
        // Retrieve the referenced entity from the EntityStore.
        var counter = await context.EntityStore.ReconstituteAsync<Counter>(id, Counter.EntityType);
        // Return NotFound() (status 404 NOT FOUND) if the entity doesn't exist.
        if (counter is null) return NotFound($"There is no counter with id [{id}]");

        // Convert command properties to domain value objects.
        var amount = new Amount(command.Amount);

        // Perform operations on the entity to change its state.
        counter.IncrementBy(amount);

        // The changes are performed by adding events. These events need to be published.
        // Publishing will fail if there are concurrent changes to the same entity. Until
        // the publishing step has completed successfully, the state of the entity cannot
        // be said to have changed.

        // Publish the changes using the EventPublisher.
        await context.EventPublisher.PublishChangesAsync(counter, actor);

        // Return 200 OK if the command was successful.
        return Ok();

        // It's not necessary to explicitly catch any exceptions. They are automatically
        // converted to 500 INTERNAL SERVER ERROR by the CommandAPI library. If you want
        // to return other
    }
}
```

Each command-handler should be annotated with exactly one of the following attributes:

- `AddsEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/`
- `DeletesEntityAttribute` - generates an HTTP endpoint on the form `DELETE /<entity>/{entityId}`
- `ModifiesEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/{entityId}/<property>`

An endpoint for retrieving the complete event history for an entity is automatically added as `GET /entity/{entityId}`.

It is the responsibliity of the `Entity` to allow or disallow specific actions based on its current state (though generally not to handle security):

```c#
using Segerfeldt.EventStore.Source;

// Entities and value objects are part of the domain model in DDD, and
// their assembly name and namespace should probably reflect that.
namespace Domain;

// Value objects are extremely useful as they increase type safety,
// thay can encapsulate validation and computation, and they can be
// compared for equality.
public sealed class Amount : ValueObject<Amount>
{
    // The amount is stored as an immutable property.
    // You should never allow mutation in a value object.
    public int Amount { get; }

    public Amount(int amount)
    {
        // Check that the input is acceptable. Throw an exception if it is not.
        // This makes it impossible to instantiate the value object with an invalid
        // amount, and Amount instances will not need further validation.
        if (amount <= 0) throw new Exception("amount must be positive");

        Amount = amount;
    }

    protected override IEnumerable<object> GetEqualityComponents() =>
        // Prefer an ImmutableArray or other immutable enumeration type.
        ImmutableArray.Create<object>(Amount);
        // The yield syntax can also be used, which is good if you have many properties.
}

// The abstract class EntityBase is a useful shortcut to implementing IEntity.
// It is not necessary to inherit from that class, but it is necessary to implement IEntity.
public sealed class Counter : EntityBase
{
    // It is recommended to define a static EntityType constant.
    public static readonly EntityType EntityType = new("Counter");

    // The constructor should usually be empty. Just call the base constructor with a constant
    // EntityType value.
    // This exact signature is expected by the EntitySource as it will need to instantiate
    // every entity before replaying its history.
    public Counter(EntityId id, EntityVersion version) : base(id, EntityType, version) { }

    // Adding a static New() method for creating new entities is recommended.
    // New entities should usually define initial state information and add events accordingly.
    public static Counter New(EntityId entityId)
    {
        // Always use EntityVersion.New as the version for new entities.
        // This indicates that the entity does not exist yet in the database.
        var counter = new User(entityId, EntityVersion.New);
        counter.Add(new UnpublishedEvent("Registered", new {}));
        return counter;
    }

    // Implement operations for manipulating the state
    public void IncrementBy(Amount amount)
    {
        // Add an UnpublishedEvent to indicate the change.
        // You could probably pass the Amount value as-is here, but it would not be
        // recommended as the domain class should always be safe to refactor.
        // The event structure however should never be allowed to change (as that would
        // make old and new details incompatible).
        Add(new UnpublishedEvent("IncrementedBy", new IncrementedByDetails(amount.Amount));
    }

    // If you need knowlegde about the current state to protect invariants, you should
    // add methods tagged with the ReplaysEventAttribute. (Their names are irrelevant
    // as long as there is no conflict.)
    [ReplaysEvent("IncrementedBy")]
    public void OnReplayOfIncrementedBy(IncrementedByDetails details)
    {
        // Update internal fields as needed to enforce domain rules.
    }

    // All methods tagged with the corresponding event name will be invoked for
    // every published event in the history.

    // Replay-methods can optionally be passed the entire PublishedEvent object
    // instead of just the details:
    [ReplaysEvent("IncrementedBy")]
    public void OnReplayOfIncrementedBy(PublishedEvent @event)
    {
        // ... but then the details will not be converted automatically.
        var details = @event.DetailsAs<IncrementedByDetails>();
        // ...
    }

    public record IncrementedByDetails(int amount);
}
```

## Projection Setup

The Projection library is meant to implement the Command-to-Query side synchronisation for a CQRS system.

Set up Projection for ASP.Net in Program.cs:

```c#
using Segerfeldt.EventStore.Projection;

builder.Services.AddSingleton<PositionTracker>();
builder.Services.AddHostedEventSource(new SqlConnectionPool(builder.Configuration.GetConnectionString("source_database")!), "source1")
    .AddReceptacles(Assembly.GetExecutingAssembly())
    .SetPositionTracker<PositionTracker>();

class SqlConnectionPool : IConnectionPool
{
    private readonly string connectionString;

    public SqlConnectionPool(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
```

See the [ProjectionWebApplication](../Apps/ProjectionWebApplication/Program.cs) for a functioning example.

## Projection Implementation

Receptacles are detected automatically in the specified assemblies. All `public` classes that implement `IReceptacle` will be notified.

```c#
using Segerfeldt.EventStore.Projection;

namespace ProjectionApp;

public record Increment(int amount);

// The abstract class ReceptacleBase is a useful shortcut to implementing IReceptacle.
// It is not necessary to inherit from that class, but it is necessary to implement IReceptacle.
public class CounterState : ReceptacleBase
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
    public void ReceiveCounterIncrement(string entityId, Increment details)
    {
        // This should usually update a projection database.
        IncrementAmountForCounterRow(entityId, details.Amount);
    }
}
```

The `PositionTracker` is used to maintain a persisted memory of your place in the event stream. When the synchronization
service is restarted it should not restart syncing from the first event. The `PositionTracker` will be notified as the
position changes so that it can update the persisted value.

```c#
using Segerfeldt.EventStore.Projection;

namespace ProjectionApp;

public class PositionTracker : IPositionTracker
{
    public long? GetLastFinishedProjectionId()
    {
        // This is called at launch (or shortly thereafter)
        // to determine which events to skip in the first update.
        return GetPersistedPosition();
    }

    public void OnProjectionStarting(long position)
    {
        // This is called before notifying receptacles
    }

    public void OnProjectionFinished(long position)
    {
        // This is called after notifying receptacles
        PersistPosition(position);
    }
}
```
