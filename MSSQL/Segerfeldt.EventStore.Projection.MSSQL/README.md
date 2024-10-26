<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Segerfeldt.EventStore.Refactoring.MSSQL

A NuGet package for performing a total redesign of a CQRS application write-model. Add this to your existing Segerfeldt.EventStore.Source application.

# Setup

You will need to set up a database connection for each write-model database (a.k.a. `EventSource`) you want to project state from. Call the extension method `IServiceCollection.UseSQLServerRefactoring(string)` to subscribe to a SQL Server write-model:

```c#
builder.Services.AddSingleton<ProjectionTracker>();
builder.Services.UseSQLServerRefactoring(builder.Configuration.GetConnectionString("current-wm")!)
    .UseSQLServerTarget(builder.Configuration.GetConnectionString("new-wm")!)
    .UseProjectionTracker<ProjectionTracker>()
    .UseTransformation<Transformation>();
```
