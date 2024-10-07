<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Segerfeldt.EventStore

A set of NuGet packages for employing event-sourcing in applications. It is particularly useful when employing the CQRS architecture style. The Command side would reference the `Source` package, and the Query side would use the `Projection` package.

State is stored in a relational database with built-in support for MS SQL Server, SQLite and PostgreSQL. See the [Tables](#tables) section for schema details.

This document is meant to help developers contribute to the source code and test their changes. There is separate documentation describing [the concept of event-sourcing](./Documentation/ES.md) and [usage in applications](./Documentation/USAGE.md).

# Build and Test

Run tests from your IDE or by using `dotnet test`.

Tests need databases and environment variables to run. Copy the sample.runsettings file to a new file named .runsettings and edit that file to match your setup. This file will not be tracked by Git so you can enter your secrets without worrying that they might be exposed on GitHub.

## PostgreSQL

The tests will fail without write-access to a running PostgreSQL test-database. Ensure that the PostgreSQL server is started and that a test database has been created before running tests.

You can download [Postgres.app](https://postgresapp.com) which is probably the easiest to run PostgreSQL on a Mac. It is also available as a [Docker image](https://hub.docker.com/_/postgres/) and by [direct installation](https://www.postgresql.org/download/).

Edit the `POSTGRES_TEST_CONNECTION_STRING` variable in .runsettings to match your PostgreSQL setup.

## MS SQL Server

The tests will fail without write-access to a running SQL Server test-database. Ensure that SQL Server is started and that a test database has been created before running tests.

SQL Server is available as a [Docker image](https://hub.docker.com/r/microsoft/mssql-server). You can also download a “[free specialized edition](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).”

Edit the `MSSQL_TEST_CONNECTION_STRING` variable in .runsettings to match your SQL Server setup.

# Testing NuGet Packages

You can test the NuGet packages without publishing them.

First set up a local NuGet store as described here:
<https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds>

Update the `<PackageVersion>` value in the .csproj file(s) and build for Release. Then run one or more of the following commands to deploy the output:

```shell
nuget add Segerfeldt.EventStore.Projection/bin/Release/Segerfeldt.EventStore.Projection.<version>.nupkg -source path/to/nuget-packages

nuget add Segerfeldt.EventStore.Source/bin/Release/Segerfeldt.EventStore.Source.<version>.nupkg -source path/to/nuget-packages

nuget add Segerfeldt.EventStore.Source.NUnit/bin/Release/Segerfeldt.EventStore.Source.NUnit.<version>.nupkg -source path/to/nuget-packages
```

To reference the NuGet packages in another solution, you will first need to configure NuGet to find your local repo.

If you have the option, you should probably use your Visual Studio IDE to set it up. But you might not have that option, or you might prefer to set it up as a config file in your Git repository so that all developers have the same configuration automatically. The configuration file looks like this:

```xml
<packageSources>
    <add key="local" value="file:///path/to/nuget/packages/" /> <!-- Not tested -->
</packageSources>
```

You can place this configuration in any of the files described in this S/O answer: <https://stackoverflow.com/a/51381519>.

Alternatively, you can use the `nuget sources` command described here: <https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-sources>.

Once you have set up the repository link correctly, you can reference it in your project using the Visual Studio IDE or by adding the following `<ItemGroup/>` to your .csproj file.

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <ItemGroup>
        <PackageReference Include="Segerfeldt.EventStore.Source.NUnit" Version="0.0.1" />
    </ItemGroup>
</Project>
```

More information about NuGet hosting and configurtion can be found here: <https://learn.microsoft.com/en-us/nuget/hosting-packages/overview>

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
