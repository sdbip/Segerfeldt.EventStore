<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Segerfeldt.EventStore.SQLite

A NuGet package for implementing the Command/Write-Model side of a CQRS application using SQLite.

Add Segerfeldt.EventStore.Projection.SQLite to another project to sync the state to a Query/Read-Model database.

# Setup

Add the following line to your Program.cs to automatically find and map endpoints for the command handlers you have defined in your main assembly.

```csharp
app.MapCommands(Assembly.GetExecutingAssembly());
```

You can optionally define your endpoints in a different assembly (or in several). Just make sure to pass them as arguments to the `MapCommands` call:

```csharp
app.MapCommands(assembly1, assembly2);
```

You will need to set up a database connection so that the commands know where to read/write their data. The [schema](#tables) will be created automatically if they haven't been added yet (assuming there are no conflicting tables in the database).

```csharp
builder.Services.UseSQLiteEventStore(builder.Configuration.GetConnectionString("main")!);
```

Add the following code to your services setup if you want Swagger documentation of your commands:

```csharp
services.AddSwaggerGen(options =>
{
    options.DocumentCommands(Assembly.GetExecutingAssembly());
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "<application name>.xml"));
});
```

You will also need to add an authentication handler to be able to identify the actor when publishing events.

```csharp
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options => {
    options.AddScheme<MyCustomAuthenticationHandler>("", "");
});
```

The `IncludeXmlComments` call is optional. If you do use it, you will need to also turn on XML documentation in your .csproj file:

```xml
<PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <DocumentationFile>bin\Release\net8.0\[application name].xml</DocumentationFile>
</PropertyGroup>
```

# Implementation

Define a command-handler by adding a class like this:

```c#
using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

// It is recommended to separate commands and entities in different assemblies, not just namespaces.
using Domain;
namespace Commands;

// The “command” is just a DTO. Execution is done by the associated CommandHandler.
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
        var actor = context.context.HttpContext.User.Identity?.Name;

        // Return status 401 UNAUTHORIZED if authentication fails.
        if (actor is null) return CommandResult.Unauthorized();

        // Return 403 FORBIDDEN if the (authenticated) user doesn't have access to run this command.
        // You might use context.HttpContext.User.IsInRole() to determine access.
        // Or you might make authorisation a part of your domain model.
        if (!IsAuthorized(actor)) return CommandResult.Forbidden();

        // See http://httpstatuses.com/ for details about response status codes.

        // The path of the request will contain the id when modifying an existing entity.
        var id = new EntityId(context.GetRouteParameter("entityid"));
        // Retrieve the referenced entity from the EntityStore.
        var counter = await context.EntityStore.ReconstituteAsync<Counter>(id, Counter.EntityType);
        // Return 404 NOT FOUND if the entity doesn't exist.
        if (counter is null) return CommandResult.NotFound($"There is no counter with id [{id}]");

        // Convert command properties to domain value objects.
        Amount amount = new Amount(command.Amount);

        try
        {
            amount = new Amount(command.Amount);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return CommandResult.BadRequest($"Command DTO is invalid: {exception.Message}")
        }

        // Perform operations on the entity to change its state.
        counter.IncrementBy(amount);

        try
        {
            // The entity will add new events to define its new state. Publish them using the EventPublisher.
            await context.EventPublisher.PublishChangesAsync(counter, actor);
        }
        catch
        {
            return
        }

        // Return 204 NO CONTENT (or 200 OK if there is a payload) if the command was successful.
        return CommandResult.NoContent();
    }
}
```

It's not necessary to explicitly catch exceptions. They will be caught by the CommandAPI infrastructure, and are automatically converted to a 500 INTERNAL SERVER ERROR response or 409 CONFLICT. You can of course catch exceptions if you want to return other status codes.

> Note: While it is possible to add a general error handler to .Net Web API, it will probably not be compatible with the CommandAPI infrastructure (it has never been a priority). It is not a recommended practice. An event handler comes with three big problems:
>
> 1. It applies the same error handling to every single request whether appropriate or not.
> 2. Middleware is confusing and hard to use due to extensive temporal coupling. The order in which it
     is added (and hence applied) tends to matter to its function.
> 3. It violates DAMP (Direct And Meaningful Phrases) by hiding away important logic from the developer.
>    DAMP is a great and useful principle that should probably be heeded more.

Each command-handler should be annotated with exactly one of the following attributes:

- `AddsEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/`
- `DeletesEntityAttribute` - generates an HTTP endpoint on the form `DELETE /<entity>/{entityid}`
- `ModifiesEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/{entityid}/<property>`

An additional endpoint for retrieving the complete event history for any entity is automatically added as `GET /entity/{entityid}`.

It is the responsibliity of the `IEntity` to allow or disallow specific actions based on its current state (though generally not to handle user privileges):

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

    // The constructor should usually be empty. Just call the base constructor with a
    // consistent (and unique to this entity class) EntityType value.
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

# Technical Notes

The point of DDD is to *not* focus on the technology or other implementation details. However, the technical choices do need to be mentioned, because developers believe they need to know them. If they actually do or not is beside the point.

## Optimistic Locking

Concurrent modification of shared state can be a big problem. If two users happen to change the same entity at the same time, there's a risk that they both read the same initial state, and then make conflicting changes that cannot be reconciled. This library employs “optimistic locking” to avoid such a scenario. Every entity has a `version` that is read when it is reconstituted, and again before publishing changes. Only if the version is the same at both instants is publishing allowed.

If the stored state is the same, it is assumed that no other process has changed the state in the intervening time. If no one has yet published new changes, there is no possibility of a conflict, and publishing the current changes will be allowed. At that time, the version is also incremented to indicate to any other active process that the state has now changed.

If the stored version number is different from what was read at reconstitution, the state has changed during the execution of this action. Since a different state can potentially affect the outcome of this action, all the current changes are to be considered invalid and publishing them is not allowed. Our only choices are to either abort the operation entirely or perform the action again. If we choose to repeat the action, we must discard the current, invalid state information, and reconstitute the entity from its new state. Then we can perform the action on this state, and try to publish those changes.

## Type Checking

Every entity in the system has a `Type` property. The `Type` property indicates what specific `EntityType` the entity has. The `EntityType` name should uniquely identify the class that implements this particular type of entity. (This is however not enforced.) When the first version (0) of an entity is added to the system, a row is added to the `Entities` table (see [the Tables Section](#tables) below). That row will include the name of the `EntityType` in the `type` column. When the entity is reconstituted by the `EventStore` that stored `type` is checked against the expected `EntityType`. If the values do not match, the `EntityStore` will return `null`.

Do not use `nameof(MyEntity)`, `entity.GetType().Name` or any other reference to the actual class name. The name of the `EntityType` must never change, even if the relevant class is renamed. The name needs to always match the `type` column for already added entities. If the `EntityType` is changed, those entities can never be reconstituted (or worse: they may be reconstituted as instances of the wrong class).

## Tables

State is stored in an SQLite database with two tables: `Entities` and `Events`.

The `Entities` table:

```sql
"id" TEXT PRIMARY KEY
"type" TEXT
"version" INT
```

The `Entities` table has two data columns: the `type` and the `version` of an entity. The version is used for concurrency checks (see [Optimistic Locking](#optimistic-locking) above). The type is used as a runtime type-checker. When reconstituting the state of an entity it needs to be the type you expect. If it isn't, an error will be thrown.

The `Events` table:

```sql
"entity_id" TEXT
"name" TEXT
"details" TEXT
"actor" TEXT
"timestamp" DECIMAL(12,7)
"ordinal" INT
"position" BIGINT
```

The events table is the main storage space for entity state. The `entity_id` column must match the `id` column for a row in the `Entities` table. This is the entity that changed with this event.

The `name` and `details` (JSON) columns define what changed for the entity. The `ordinal` column orders events per entity. The `position` column orders events globally and is mostly used for projections.

The `actor` and `timestamp` columns are metadata that can be used for auditing. The `timestamp` is stored as the number of days (including fraction) that have passed since midnight UTC on Jan 1, 1970 (a.k.a. the Unix Epoch).
