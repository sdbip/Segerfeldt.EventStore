# Segerfeldt.EventStore

A package for using event-sourcing in applications. It is particularly useful when using the CQRS architecture style. The Command side would employ the `Source` package, and the Query side would use the `Projection` package.

State is stored in a relational database with built-in support for MS SQL Server, SQLite and PostgreSQL.

# Build and Test

Run tests from your IDE or by using `dotnet test`.

Tests need environment variables to run. Copy the sample.runsettings file to a new file named .runsettings and edit that file to match your setup. This file will not be tracked by Git so you can enter your secrets without worry.

## PostgreSQL

The tests will fail without write-access to a running PostgreSQL test-database. Ensure that the PostgreSQL server is started, and that a test database has been created, before running tests.

You can download [Postgres.app](https://postgresapp.com) which is probably the easiest to run PostgreSQL on a Mac. It is also available as a [Docker image](https://hub.docker.com/_/postgres/) and by [direct installation](https://www.postgresql.org/download/).

Edit the `POSTGRES_TEST_CONNECTION_STRING` variables in .runsettings to match your PostgreSQL setup.

## MS SQL Server

The tests will fail without write-access to a running SQL Server test-database. Ensure that SQL Server is started, and that a test database has been created, before running tests.

SQL Server is available as a [Docker image](https://hub.docker.com/r/microsoft/mssql-server). You can also download a “[free specialized edition](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).”.

Edit the `MSSQL_TEST_CONNECTION_STRING` variables in .runsettings to match your SQL Server setup.

# Testing NuGet Packages

You can test the NuGet package without publishing it.

First set up a local NuGet store as described here:
https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds

Update the `<PackageVersion>` value in the .csproj file(s) and build for Release. Then run one or more of the following commands to deploy the output:

```shell
nuget add Segerfeldt.EventStore.Projection/bin/Release/Segerfeldt.EventStore.Projection.<version>.nupkg -source path/to/nuget-packages

nuget add Segerfeldt.EventStore.Source/bin/Release/Segerfeldt.EventStore.Source.<version>.nupkg -source path/to/nuget-packages

nuget add Segerfeldt.EventStore.Source.NUnit/bin/Release/Segerfeldt.EventStore.Source.NUnit.<version>.nupkg -source path/to/nuget-packages
```

To reference the NuGet package in another solution, you will first need to configure NuGet to find your local repo.

If you have the option, you should probably use your Visual Studio IDE to set it up. But you might not have that option, or you might prefer to set it up as a config file in your Git repository so that all developers have the same configuration automatically. The configuration file looks like this:

```xml
<packageSources>
    <add key="local" value="file:///path/to/nuget/packages/" /> <!-- Not tested -->
</packageSources>
```

You can place this configuration in any of the files described in this S/O answer: https://stackoverflow.com/a/51381519.

Alternatively, you can use the `nuget sources` command described here: https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-sources.

Once you have set up the repository link correctly, you can reference it in your project using the Visual Studio IDE or by adding the following `<ItemGroup/>` to your .csproj file.

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <ItemGroup>
        <PackageReference Include="Segerfeldt.EventStore.Source.NUnit" Version="0.0.1" />
    </ItemGroup>
</Project>
```

More information about NuGet hosting and configurtion can be found here: https://learn.microsoft.com/en-us/nuget/hosting-packages/overview

# Usage

## Source

The Source library is meant to implement the Command side (a.k.a. the write model) of a CQRS system. Import this in your web service code to start manipulating entities and publishing events.

Add the following line to your Program.cs to automatically find and map endpoints for the command handlers you have defined in your main assembly.

```csharp
app.MapCommands(Assembly.GetExecutingAssembly());
```

You can optionally define your endpoints in a different assembly (or in several). Just make sure to pass them as agruments to the `MapCommands` call:

```csharp
app.MapCommands(assembly1, assembly2);
```

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
namespace Domaim;

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

See the [Example apps](#examples) for more usage examples.

Add the following code to your setup to add Swagger documentation:

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

## Projection

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

The `PositionTracker` is used to maintain a persisted memory of your place in the event stream. When the synchronization
service is restarted it should not restart syncing from the first event. The `PositionTracker` will be notified as the
position changes so that it can update the persisted value.

Receptacles are detected automatically. All classes, in the specified assemblies, that implement `IReceptacle` will be notified.

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

# The Concept Behind Event Sourcing

By focusing on how the state *changes*, we can better understand how our domain works.

The idea of event sourcing is to not simply store the current *state* of the application, but instead store each historical *change* to the state. We call such changes ‘events.’

There are some benefits to using this idea; the most obvious ones are perhaps immutability and auditing. When an event has been recorded, that historical information will itself never change. It makes referring to the data much simpler, and you need never worry about concurrent updates. Every event also records a timestamp and a username which can be very useful metadata for auditing.

Event sourcing also allows creating independent *projections* of the state. You can replay all the changes at any time, and maintain a different storage location with an alternate view into the data. For example you can gather all the current state for easy indexing and quick access. You can ignore a lot of the information, and focus on generating the data structure that makes your particular use case simple and performant.

Event Sourcing is a product of Domain-Driven Design (DDD). In DDD, we have two carriers of state: the value object and the entity.

## Value Objects

A value object is (as the term implies) an object that represents a specific value. Values never change; you can only replace a value with a new one. Value objects are therefore always immutable.

Value objects typically have two functions: they can be compared for structural equality, and they can be validated for correct user input.

It should not be possible to instantiate an invalid value object. The constructor (or factory) should prevent such, typically by throwing an exception or returning a failure result (see for example Vlad Khorikov's [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions/blob/master/CSharpFunctionalExtensions/Result/Result.cs)) when invalid data is encountered. If the programmer can rely on this working, they will not need to validate the data in their code; it is enough to declare that a variable must be of the correct type (and not `null`).

Value objects can be used in sets and as dictionary keys. A mutable object used as a dictionary key can be changed after it's added, altering its hash-code and making it impossible to find again. Immutability is necessary to guarantee correct behaviour.

Value objects can also be used in calculations. You might for example `Add()` (or `+`) two value objects of same type to get their sum. Or you might multiply a value object with a scalar (e.g. a `Money` amount and an interest rate). The result of a calculation is typically an object of the same type as the input, but it could be otherwise. `Ingredient1` combined with `Ingredient2` might for example produce a `Cake`.

Value objects should also be encapsulated. They should have an internal representation of data and an external interface. Users of the value object should only ever couple to the interface, never to the concrete data representation. That allows the storage strategy to change without breaking references to the value object. And it also helps the programmer stay focused on the *meaning* of the value rather than its *composition*.

Should, for example, the data representation of an amount of `Money` be composed of an `Int` counting the cents (100 meaning one dollar), a `Double` value of dollars (0.5 to represent 50 cents) or two separate `Int` values (one for dollars and one for cents, where cents < 100)? If you ever need to change the data format to support new use cases or higher precision, encapsulation is a guard against errors in all code outside the `Money` type itself.

## Entities

Not everything can be made immutable. If it were, we would have little (if any) use of software. We need to gather new data, and update existing data. We need to support business processes and user tasks, both of which rely heavily on *changing* the data stored in the system. Without data changes, the usefulness of the system would be very limited.

But rather than manipulating *data*, in its specific format and structure, Domain-Driven Design (DDD) teaches us to focus on what that data *means* conceptually and/or metaphorically. And why and how it changes. The formatting and structure of the data can be altered in many ways and still represent the same meaning; the same state.

DDD separates the state of the system into parts called entities; the state of the system is the aggregated state of all entities. Each `Entity` defines invariants that must be maintained. Like value objects, entities should be encapsulated. The interface of an `Entity` should only expose meaningful operations, not direct state/data manipulation. If an action executes an operation that is invalid/unsupported for its current state, the `Entity` should throw an exception.

Unlike value objects, entities possess an identifier. Since the `Entity` is stateful, there must be a way to identify which entity to modify. To summarise there are three main differences between value objects and entities:

1. Value objects are immutable, while entities are stateful.
2. A value object has meaning, while an entity has an identity.
    - Comparisons between value objects look at their entire structure,
      while comparisons between entities only concern their ids.
3. Value objects are often and easily discarded and recreated, while entities live on for a long time.

## Events

DDD teaches us to focus on how the business processes *change* the state rather than just what the state is at any given time. By focusing on how and why the state changes we can better understand how our domain works.

This library employs event sourcing, which means that we define the state of an entity by listing all the changes in order that have been made to it since it was first added to the system/application. These changes are commonly referred to as `Event`s. Events specify the conceptual meaning of each change and lists its specific details. It also includes which user caused the change and at what time the `Event` was published.

By storing all events in a way that persists their chronology, the current state of the entire system is well defined. In functional programming terms the state of each `Entity` can be implemented as a simple `fold()` operation. The state of the complete system being the aggregated state of all entities is then the `fold()` operation mapped over the list of lists that is the events of all entities.

A CQRS solution can then synchronise (project) the events into a searchable query database. When the projection/query database is first set up, all the existing events should be processed in order. As new events are then published, the projection server should pick up those events and update the projection/query database accordingly.

And it is also well defined what the state was at any point of time in the past. This fact can be used to debug the system. The events up to a given point in time can be copied to a new database and then used to recreate the system state at that time. Then experimentation can find the bug allowing it to be fixed.

Projection can be used for other purposes than the Query side of CQRS. It can for example be used to communicate between bounded contexts. Or it can be used to generate one-off reports. Or myriad other things.

## What About Aggregates?

In his “Big Blue Book,” Eric Evans defines the term “aggregate.” This refers to a collection of entities that can only exist meaningfully as a group. This group always has an “aggregate root” which is one of the contained entities. (It can also be called “the root entity.”) Other event sourcing libraries add a class named `Aggregate` to refer to such groups, and to generate the events that define them. This is however a confusing term as it literally refers to the relationship between entities, but conceptually to the group as an object.

The aggregate is called `Entity` in this library; mostly because every aggregate is clearly defined by referencing its root entity making the confusing term redundant. It has similar purpose as the `Aggregate` class in other libraries, but it doesn't introduce a third term for modelling. (The terms “entity” and “value object” are quite enough, thank you.)

Other libraries often treat the `Aggregate` much like a list of commands rather than a modelling object. Commands often perform multiple operations on an entity in one batch. This is not how `Entity` is used here. Instead commands are assumed to be external to the model, and they can freely compose what operations to perform (or manipulate multiple entities) before publishing the generated events.

The aggregate *rule* still applies though. Entities that belong to an aggregate relationship can only be accessed through the root entity. It should not be possible to access a child entity without its parent. It is however up to the developer to define these entity classes and invariants, and to couple them to the root entity so that they can add events (and replay them correctly when necessary for maintaining internal cnsistency).

There is an example of how an aggregate might be constructed in the test case `Segerfeldt.EventStore.Source.Tests.AggregateTests`. Please do not take it as canon; you own implementation will probably be a better match for your purpose.

# Examples

See the Apps/ directory for example applications.

- ProjectionWebApplication: an example of subscribing to published events in order to track aggregate state for multiple entities
- SourceConsoleApp: an example of direct (simpler) generation and publishing of events
- SourceWebApplication: a complete (more complex) example of publishing of events through command handlers in a web application

# Technical Notes

The point of DDD is to *not* focus on the technology or other implementation details. However, the technical choices do need to be mentioned, because developers believe they need to know them. If they actually do or not is beside the point.

## Optimistic locking

Concurrent modification of shared state can be a big problem. If two users happen to change the same entity at the same time, there's a risk that they both read the same initial state, and then make conflicting changes that cannot be reconciled. This library employs “optimistic locking” to avoid such a scenario. Every entity has a `version` that is read when it is reconstituted, and again before publishing changes. Only if the version is the same at both instants is publishing allowed.

If the stored state is the same, it is assumed that no other process has changed the state in the intervening time. If no one has yet published new changes, there is no possibility of a conflict, and publishing the current changes will be allowed. At that time, the version is also incremented to indicate to any other active process that the state has now changed.

If the stored version number is different from what was read at reconstitution, the state has changed during the execution of this action. Since a different state can potentially affect the outcome of this action, all the current changes are to be considered invalid and publishing them is not allowed. Our only choices are to either abort the operation entirely or perform the action again. If we choose to repeat the action, we must discard the current, invalid state information, and reconstitute the entity from its new state. Then we can perform the action on this state, and try to publish those changes.

## Tables

State is stored in a relational database with built-in support for MS SQL Server, SQLite and PostgreSQL. Note that table names are never quoted in the SQL scripts, and, because reasons, PostgreSQL converts unquoted names to lowercase. That is however the only difference you will see.

The table layout is the exact same as used by the Swift EventSourcing package. A write model generated by a Swift web application should be 100% compatible with the C# Projection library defined here. And vice versa too.

The `Entities` table:

```sql
"id" TEXT PRIMARY KEY
"type" TEXT
"version" INT
```

The `Entities` table has two data columns: the `type` and the `version` of an entity. The version is used for concurrency checks (see Optimistic Concurrency above). The type is used as a runtime type-checker. When reconstituting the state of an entity it needs to be the type you expect. If it isn't, an error will be thrown.

The `Events` table:

```sql
"entity_id" TEXT
"name" TEXT
"details" TEXT
"actor" TEXT
"timestamp" DECIMAL(12,7)
"version" INT
"position" BIGINT
```

The events table is the main storage space for entity state. The `entity_id` column must match the `id` column for a row in the `Entities` table. This is the entity that changed with this event.

The `name` and `details` (JSON) columns define what changed for the entity. The `version` column orders events per entity. The `position` column orders events globally and is mostly used for projections.

The `actor` and `timestamp` columns are metadata that can be used for auditing.

The `timestamp` is stored as the number of days (including fraction) that have passed since midnight UTC on Jan 1, 1970 (a.k.a. the Unix Epoch).

## Database Support

There is currently support for three database providers.

- MS SQL Server
- PostgreSQL
- SQLite
