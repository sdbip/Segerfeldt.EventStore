# Segerfeldt.EventStore

A package for using event-sourcing in applications. It is particularly useful when using the CQRS architecture style. The Command side would employ the `Source` package, and the Query side would use the `Projection` package.

State is stored in a relational database with built-in support for MS SQL Server, SQLite and PostgreSQL.

## Commanding

EventStore can auto-generate RESTful HTTP endpoints from your command handlers. Add the following code to your setup to trigger a search for command handler classes throughout your assembly:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapCommands(Assembly.GetExecutingAssembly());
});
```

An endpoint for retrieving the complete event history for an entity is also added as `GET /entity/{entityId}`.

Each command-handler should be annotated with exactly one of the following attributes:
- `AddsEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/`
- `DeletesEntityAttribute` - generates an HTTP endpoint on the form `DELETE /<entity>/{entityId}`
- `ModifiesEntityAttribute` - generates an HTTP endpoint on the form `POST /<entity>/{entityId}/<property>`

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

The `IncludeXmlComments` call is optional. If you use it, you need to also turn on XML documentation in your .csproj file:

```xml
<PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <DocumentationFile>bin\Release\net7.0\[application name].xml</DocumentationFile>
</PropertyGroup>
```

# The Concept Behind Event Sourcing

By focusing on how the state *changes*, we can better understand how our domain works.

The idea of event sourcing is to not simply store the current *state* of the application, but instead store each historical *change* to the state. We call such changes ‘events.’

There are some benefits to using this idea; the most obvious ones are perhaps immutability and auditing. When an event has been recorded, that historical information will itself never change. It makes referring to the data much simpler, and you need never worry about concurrent updates. Every event also records a timestamp and a username which can be very useful metadata for auditing.

Event sourcing also allows creating independent *projections* of the state. You can replay all the changes at any time, and maintain a different storage location with an alternate view into the data. For example you can gather all the current state for easy indexing and quick access. You can ignore a lot of the information, and focus on generating the data structure that makes your particular use case simple and performant.

Event Sourcing is a product of Domain-Driven Design (DDD). In DDD, we have two carriers of state: the value object and the entity.

## Value Objects

Value objects are not modeled by this library, but it is still important to understand them.

A value object is (as the term implies) an object that represents a specific value. Values never change; you can only replace a value with a new one. Value objects are therefore always immutable.

Value objects typically have two functions: they can be compared for structural equality, and they can be validated for correct user input.

It should not be possible to instantiate an invalid value object. The initializer should prevent such, typically by throwing an exception or returning `nil` when invalid data is encountered. If the programmer can rely on this working, they will not need to validate the data in their code; it is enough to declare that a variable must be of the correct type (and not `nil`).

Most value objects conform to `Equatable` and `Hashable`. They can thus be used in sets and as dictionary keys. A mutable object used as a dictionary key can be changed after it's added making it impossible to find again. Immutability is necessary to guarantee correct behaviour.

`Equatable` value objects can also be `Comparable` and they can be used in calculations. You might for example add (`+`) two value objects of same type to get their sum. Or you might multiply a value object with a scalar (e.g. an interest rate). The result of a calculation is typically an object of the same type as the input, but it could be otherwise. E.g. `Ingredient1` plus `Ingredient2` might return a `Cake`.

Value objects should also be encapsulated. They should have an internal representation of data and an external interface. Users of the value object should only ever couple to the interface, never to the concrete data representation. That allows the storage strategy to change without breaking references to the value object. And it also helps the programmer stay focussed on the *meaning* of the value rather than its *composition*.

Should, for example, the data representation of an amount of `Money` be composed of an `Int` counting the cents (100 meaning one dollar), a `Double` value of dollars (0.5 to represent 50 cents) or two separate `Int` values (one for dollars and one for cents, where cents < 100)? If you ever need to change the data format to support new use cases or a need for higher precision, encapsulation is a guard against errors in all code outside the `Money` type itself.

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

DDD teaches us to focus on how the business processes *change* the state rather than just what the state is at any given time. By focusing on how the changes, and the reason for the changes, we can better understand how our domain works.

This library employs event sourcing, which means that we define the state of an entity by listing the changes that has happened to it since it was first added to the system/application. These changes are commonly referred to as `Event`s. Events specify the details of how the state changed, which user caused it and at what time the change occurred.

By storing all events in a way that persists their chronology, the current state of the entire system is well defined. In functional programming terms the state of each `Entity` can be implemented as a simple `fold()` operation. The state of the complete system being the aggregated state of all entities is then the `fold()` operation mapped over the list of lists that is the events of all entities.

A CQRS solution can then synchronise (project) the events into a searchable query database. When the projection/query database is first set up, all the existing events should be processed in order. As new events are then published, the projection server should pick up those events and update the projection/query database accordingly.

And it is also well defined what the state was at any point of time in the past. This fact can be used to debug the system. The events up to a given point in time can be copied to a new database and then used to recreate the system state at that time. Then experimentation can find the bug allowing it to be fixed.

Projection can be used for other purposes than the Query side of CQRS. It can for example be used to communicate between bounded contexts. Or it can be used to generate one-off reports. Or myriad other things.

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

The table layout is the exact same as used by the C# NuGet package Segerfeldt.EventSourcing. A write model generated by a C# web application should be 100% compatible with the Swift Projection target defined here. And vice versa too.

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
"entity_type" TEXT
"name" TEXT
"details" TEXT
"actor" TEXT
"timestamp" DECIMAL(12,7)
"version" INT
"position" BIGINT
```

The events table is the main storage space for entity state. The `entity_id` and `entity_type` columns must match the corresponding columns for a row in the `Entities` table. This is the entity that changed with this event.

The `name` and `details` (JSON) columns define what changed for the entity. The `version` column orders events per entity. The `position` column orders events globally and is mostly used for projections.

The `actor` and `timestamp` columns are metadata that can be used for auditing.

The `timestamp` is stored as the number of days (including fraction) that have passed since midnight UTC on Jan 1, 1970 (a.k.a. the Unix Epoch).

## Database Support

There is currently support for three database providers.

- MS SQL Server
- PostgreSQL
- SQLite

# Deploying to NuGet (local)

Set up a local NuGet store as described here:
https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds

Update the `<PackageVersion>` value in the .csproj file(s) that you want to deploy and build for Release. Then run one or more of the following commands to deploy the output:

```shell
nuget add Segerfeldt.EventStore.Projection/bin/Release/Segerfeldt.EventStore.Projection.<version>.nupkg -source path/to/nuget-packages

nuget add Segerfeldt.EventStore.Source/bin/Release/Segerfeldt.EventStore.Source.<version>.nupkg -source path/to/nuget-packages

nuget add Segerfeldt.EventStore.Source.NUnit/bin/Release/Segerfeldt.EventStore.Source.NUnit.<version>.nupkg -source path/to/nuget-packages
```
