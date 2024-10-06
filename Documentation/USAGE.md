# Segerfeldt.EventSourcing Usage

This document is meant to help develop clients that employ the Segerfeldt.EventSourcing NuGet packages. If you are not familiar with event sourcing, you should probably read the [documentation describing the concepts](./ES.md) first.

See the Apps/ directory for example applications.

- ProjectionWebApplication: an example of subscribing to published events in order to track aggregate state for multiple entities
- SourceWebApplication: as example of publishing of events through command handlers in a web application
- SourceConsoleApp: an example of direct (simpler) generation and publishing of events

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
    <DocumentationFile>bin\Release\net8.0\[application name].xml</DocumentationFile>
</PropertyGroup>
```

See the [SourceWebApplication](../Apps/SourceWebApplication/Program.cs) for a functioning example.

## Source Implementation

Define a command-handler by adding a class like this:

```c#
using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

// It is recommended to separate commands and entities in different assemblies, not just namespaces.
using Domain;
namespace Commands;

public record IncrementCounter(int amount);

// Implement one of the ICommandHandler interfaces to declare a command handler. The
// `ModifiesEntityAttribute` (and its subclasses) defines the path pattern and the
// verb/method for the command's HTTP endpoint.
[ModifiesEntity("Counter")]
public sealed class IncrementCounterCommandHandler : ICommandHandler<IncrementCounter>
{
    public async Task<CommandResult> Handle(IncrementCounter command, CommandContext context)
    {
        // The actor is the user that executes the command.
        // The name of the current principal is usually a good choice.
        var actor = context.HttpContext.User?.Name;

        // Return status 401 UNAUTHORIZED if authentication fails.
        if (actor is null) return CommandResult.Unauthorized();
        // Return 403 FORBIDDEN if user doesn't have access to this command.
        if (!IsAuthorized(actor)) return CommandResult.Forbidden();

        // See http://httpstatuses.com/ for details about response status codes.

        // The path of the request contains the id when modifying an existing entity.
        var id = new EntityId(context.GetRouteParameter("entityid"));
        // Retrieve the referenced entity from the EntityStore.
        var counter = await context.EntityStore.ReconstituteAsync<Counter>(id, Counter.EntityType);
        // Return 404 NOT FOUND if the entity doesn't exist.
        if (counter is null) return CommandResult.NotFound($"There is no counter with id [{id}]");

        // Convert command properties to domain value objects.
        var amount = new Amount(command.Amount);

        // Perform operations on the entity to change its state.
        counter.IncrementBy(amount);

        // The entity will add new events to define its new state. Publish them using the EventPublisher.
        await context.EventPublisher.PublishChangesAsync(counter, actor);

        // Return 204 NO CONTENT (or 200 OK if there is a payload) if the command was successful.
        return CommandResult.NoContent();

        // It's not necessary to explicitly catch exceptions. They will be caught implicitly
        // by the CommandAPI infrastructure, and are automatically converted to 500 INTERNAL
        // SERVER ERROR. If you want to return a different status however, you will of course
        // have to catch the exceptions.
    }
}
```

> Note: While it is possible to add a general error handler to .Net Web API (and while it would probably be possible to make the CommandAPI system compatible), it is not a recommended practice. An event handler has three big problems:
>
> 1. It applies the same error handling to every single request whether appropriate or not.
> 2. Middleware is confusing and hard to use, as the order in which it is added (and hence applied) tends to matter to its function.
> 3. It violates DAMP (Direct And Meaningful Phrases) by hiding away important logic from the developer.
>    DAMP is a great and useful principle that should probably be heeded more.

Each command-handler should be annotated with exactly one of the following attributes:

- `AddsEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/`
- `DeletesEntityAttribute` - generates an HTTP endpoint on the form `DELETE /<entity>/{entityid}`
- `ModifiesEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/{entityid}/<property>`

An additional endpoint for retrieving the complete event history for any entity is automatically added as `GET /entity/{entityid}`.

It is the responsibliity of the `Entity` to allow or disallow specific actions based on its current state (though generally not to handle user privileges):

```c#
using Segerfeldt.EventStore.Source;

// Entities and value objects are part of the domain model in DDD, and
// their assembly name and namespace should probably reflect that.
namespace Domain;

// Value objects are extremely useful as they increase type safety, they can
// encapsulate validation and computation, and they can be compared for equality.
public sealed class Amount : ValueObject<Amount>
{
    // The amount value is stored as an immutable property.
    // You should never allow mutation in a value object.
    public int Value { get; }

    private Amount(int value)
    {
        // Check that the input is acceptable. Throw an exception if it is not.
        // This makes it impossible to instantiate the Amount object with an invalid
        // value, and Amount instances will need no further validation.
        if (value <= 0) throw new ArgumentOutOfRangeException("Amount value must be positive");

       Value = value;
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
        Add(new UnpublishedEvent("IncrementedBy", new IncrementedByDetails(amount.Value));
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

internal sealed class SqlConnectionPool : IConnectionPool
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

The `PositionTracker` is used to maintain a persisted memory of your place in the event stream. When the synchronization
service is restarted it should not restart syncing from the first event. The `PositionTracker` will be notified as the
position changes so that it can update the persisted value.

```c#
using Segerfeldt.EventStore.Projection;

namespace ProjectionApp;

public sealed class PositionTracker : IPositionTracker
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
