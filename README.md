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

The idea of event sourcing is to not simply store the current *state* of the application, but instead store each historical *change* to the state. We call such changes *events*.

There are some benefits to using this idea; the most obvious ones are perhaps immutability and auditing. When an event has been recorded, that information will itself never change. It makes referring to the data much simpler, and you need never worry about concurrent updates. Every event also records a timestamp and a username. This can be very useful metadata for auditing and where to invest in more education.

Event sourcing also allows creating independent *projections* of the state. You can replay all the changes at any time, and maintain a different storage location with an alternate view into the data. For example you can gather all the current state for easy indexing and quick access. You can ignore a lot of the information, and focus on generating the data structure that makes your particular use case simple and performant.

Event Sourcing is a product of Domain-Driven Design (DDD). In DDD, we have two carriers of state: the value object and the entity.

## Value Objects

A value object (as the term implies) is an object that represents a specific value. Values cannot be modified, only replaced. For example, the value $3 is always $3; it will never become $4, which is *a different value*. A *variable* that contains the value $3 might be reassigned the value $4, but *the value* $3 can never become $4.

Because of parallel execution, mutable objects might change in the middle of a calculation, but immutable objects can *never* change. Value objects are therefore always immutable. Immutability is an idea that comes from Functional Programming. It means that objects may be passed around to different parts of your code without risk of them disrupting each other.

Value objects are also encapsulated. They have an internal representation of data and an external interface. Users of the value object may only couple to the interface, not to the data representation.

Coupling to the data structure makes your code brittle. And consequently rigid. It is brittle because even the smallest structural change can disrupt the functionality of your application. It is rigid because you will not want to cause such disruption. By decoupling from the structure your code becomes more supple. This form of encapsulation is called Object Orientation.

Encapsulation is not just about coupling; it's also about usage. Objects have *invariants*. An invariant is a property that must always be true. “The email address must always contain an at sign (@),” “a password must always be strong enough,” etc. Data structures have to be validated before they are persisted, but value objects do not. You validate the input data in the constructor of a value object. Make sure cannot be constructed with invalid data and you will never have to validate it again. If you possess an instance you can simply use it.

> Note: This tool cannot ensure encapsulation. You will have to do that yourself. This is typically done by throwing appropriate exceptions in the constructor if the input data is invalid.

There are three main use-cases for value objects:

- Validation: the value object is invalid if its invariants are not met. Enforce correctness in the constructor.
- Calculations: value objects can be combined with each other (or with other values) to generate new value objects (or values).
- Equality and comparisons (and hash-lookup): value objects can be compared with each other (`$3 < $4`) and tested for equality (`$3 == $3 && $3 != $4`). Two instances of $3 are always equal; it does not matter how you created them.

   And (being immutable themselves) they will each have an immutable hash-code and can be used in sets and as keys in dictionaries.

## Entities

Not everything can be made immutable. If it were, we would have little (if any) use of software. We need to gather new data, and update existing data. We need to support business processes and user tasks, both of which heavily rely on *changing* the data stored in the system. Domain-driven design (DDD) was formulated in part to focus on these processes, rather than the data they manipulate. In DDD we do not focus on the data as such, but on what the data *represents*.

An *entity* is an object that has state. State is information (data) about the current reality of a particular thing. That “thing” is the entity. It might be a physical “thing” (eg. a person, a vehicle, a device, etc) or it might be non-physical (like a project, a document, a department of our company...).

Like value objects, entities are encapsulated. Users of the entity should focus on *operating* the entity, not what state each operation generates. The state is maintained by the entity itself.

> Note: This tool cannot ensure encapsulation. You will have to do that yourself. That is part of designing your model. The typical approach is to throw appropriate exceptions if an operation is not supported given the current state. To avoid operations that would lead to an invalid state, exceptions is actually *not* the best approach. Rather the typical approach is to expose a different interface, such as replacing two setters with a single method that accepts two parameters.

## Events

By focusing on how the state *changes*, we can better understand how our domain works.

This library employs event sourcing, which means that we define the state of an entity by listing the changes that has happened to it since it was first added to the system/application. These changes are commonly referred to as *events*. The state of the entity hasn't officially changed until the events are published. When they have been published, they are forever a part of the entity's history. They are never changed or removed. The history up to that point will never change. Any new events will always be appended to the end of the history.

# Examples

See the Apps/ directory for example applications.

- ProjectionWebApplication: an example of subscribing to published events in order to track aggregate state for multiple entities
- SourceConsoleApp: an example of direct (simpler) generation and publishing of events
- SourceWebApplication: a complete (more complex) example of publishing of events through command handlers in a web application

# Technical Notes

The point of DDD is to *not* focus on the technology or other implementation details. However, the technical choices do need to be mentioned, because developers believe they need to know them. If they actually do or not is beside the point.

## Optimistic locking

Concurrent modification of shared state can be a big problem. If two users happen to change the same entity at the same time, there's a risk that they both read the same initial state, and then make conflicting changes that cannot be reconciled. This library employs “optimistic locking” to avoid such a scenario. Every entity has a `Version` that is read when it is reconstituted, and again before publishing changes. Only if the version is the same at both instants is publishing allowed.

If the stored state is the same, it is assumed that no other process has changed the state in the intervening time. If no one has yet published new changes, there is no possibility of a conflict, and publishing the current changes will be allowed. At that time, the version is also incremented to indicate to any other active process that the state has now changed.

If the stored version number is different from what was read at reconstitution, the state has changed during the execution of this action. Since a different state can potentially affect the outcome of the action, all the current changes are to be considered invalid and publishing them is not allowed. Our only choice is whether to abort the operation entirely or perform the action again. If we choose to repeat the action, we must discard the invalid state information and reconstitute the entity from its new state. Then we can repeat the action from the updated state, and try to publish those changes.

## Tables

State is stored in a relational database with built-in support for MS SQL Server, SQLite and PostgreSQL. Note that table names are never quoted in the SQL scripts, and, because reasons, PostgreSQL converts unquoted names to lowercase. That is however the only difference you will see.

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

The `name` and `details` (JSON) columns define what changed for the entity. The `version` column orders events per entity, and the last event stored for an entity must match its `version` column. The `position` column orders events globally and is mostly used for projections.

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
