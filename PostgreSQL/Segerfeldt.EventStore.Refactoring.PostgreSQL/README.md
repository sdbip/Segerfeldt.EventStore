<!--
    This comment only exists to disable the Markdownlint rule
    MD025/single-title/single-h1: Multiple top-level headings in the same document
    This behaviour was observed when using https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint
-->

# Segerfeldt.EventStore.Refactoring.PostgreSQL

A NuGet package for performing a total redesign of a CQRS application write-model. Add this to your existing Segerfeldt.EventStore.Source application to generate a parallel write-model database representing the same history in a different language.

Once an event has been recorded in an event database, its structure must never be redesigned. Doing so would render it incompatible with existing projections as well as the domain model itself. But sometimes needs must. In order to continue development, the event database might need to be redesigned. In that situation, follow a strategy like the following (see [The Complete Strategy](#the-complete-strategy) for a more detailed description):

1. Create a new (target) write-model database that duplicates the entire history of the source, but using the new event model.
2. Run both databases in parallel for some time and update the target database as events are published to the source.
3. Translate the domain model to use the new model (but don't deploy).
4. Test the translation and the corresponding domain changes thoroughly.
5. Reverse the roles of the databases, making the new database the source of new events and the old database the target of a reverse translation.
6. Incrementally update your projections to use the new model instead of the old.
7. When all projections are switched to the new model, you can safely shelve the old database.

This package is meant to aid the refactoring process. It specifically addresses steps 1, 2 and 5 in the above list.

# Setup

You will need to set up a database connection for each write-model database (a.k.a. `EventSource`) you want to project state from. Call the extension method `IServiceCollection.UsePostgreSQLRefactoring(string)` to subscribe to a PostgreSQL write-model:

```csharp
builder.Services.UsePostgreSQLRefactoring(builder.Configuration.GetConnectionString("old-event-model")!)
    .UsePostgreSQLTarget(builder.Configuration.GetConnectionString("new-event-model")!)
    .UseProjectionTracker<MyCustomProjectionTracker>()
    .UseTransformation<MyCustomTransformationStrategy>();
```

You do not have to have PostgreSQL on both sides of the refactoring. You can for example copy an PostgreSQL event model to your custom database:

```csharp
builder.Services.UsePostgreSQLRefactoring(builder.Configuration.GetConnectionString("old-event-model")!)
    .UseTarget(p => new MyCustomConnection(builder.Configuration.GetConnectionString("new-event-model")!))
    .UseProjectionTracker<MyCustomProjectionTracker>()
    .UseTransformation<MyCustomTransformationStrategy>();
```

Or you could copy from your custom database to SQL Server:

```csharp
builder.Services.UseRefactoring(p => new MyCustomConnection(builder.Configuration.GetConnectionString("old-event-model")!))
    .UsePostgreSQLTarget(builder.Configuration.GetConnectionString("new-event-model")!)
    .UseProjectionTracker<MyCustomProjectionTracker>()
    .UseTransformation<MyCustomTransformationStrategy>();
```

For step 5, revese the relationship:

```csharp
builder.Services.UsePostgreSQLRefactoring(builder.Configuration.GetConnectionString("new-event-model")!)
    .UsePostgreSQLTarget(builder.Configuration.GetConnectionString("old-event-model")!)
    // There is no need to change the ProjectionTracker; assuming that the models are in sync,
    // the reversal should be able to just pick up at the same position
    .UseProjectionTracker<MyCustomProjectionTracker>()
    // ...but the TransformationStrategy will need to be reversed
    .UseTransformation<MyCustomReverseTransformationStrategy>();
```

The `ProjectionTracker` is meant to persist the current position in the event stream so that it can be recovered after a restart.

Implement your `TransformationStrategy` to generate new events to replace the old. And your `ReverseTransformationStrategy` do do the opposite.

```csharp
class TransformationStrategy : ITransformationStrategy
{
    public IEnumerable<TranslatedEvent> TransformPublishedBatch(IEnumerable<SourceEvent> sourceEvents)
    {
        // This simplistic implementation creates a direct copy of the existing event model.
        // You will likely need to do something a bit more complex.
        return sourceEvents.Select(e => e.Unchanged);

        // The sourceEvents were all generated at the same time, by the same actor in the same call to EventPublisher.Publish().
        // (Or they have been translated from events that were.) They are guaranteed to have the same position, timestamp and actor.
        // In the target even model they will have the same position, timestamo and actor. Their ordinal position will however depend
        // entirely on their relative position in the returned list.

        // You might want to inspect all the events rather than simpistically iterating through them.
        // For example you might want to merge two or mor events into one.

        // To leave a source event as it is you can use SourceEvent.Unchanged as in this example.
    }
}
```

# The Complete Strategy

This process is meant to span an extended time-frame (possibly weeks or months). Any two steps in this list are unlikely to occur on the same day.

## Step 1: Copy the State into a New (Target) Event Model

Create a new (target) write-model database that duplicates the entire history of the source, but using the new event model. Maintain the order of events as they were in the source. And keep the same metadata (`timestamp`, `position` and `actor`) for the translated events as for their respective source events.

Go through all the events of the current write-model just like you would a projection, but instead of updating a row in a table, create new event data that represents the exact same state change in the new terminology. Allow changes to its `details` structure and `name`, and possibly even which entity it belongs to. Maybe you'll want to split some events in two or more, or merge several events into one.

Make sure that you can reverse the translation as well. You will need to maintain the old model for some time to allow projections to keep working.

## Step 2: Run Both Models in Parallel for Some Time

At the inception you will need the new model to be a projection of the old. But after a while (probably minutes really) it will have caught up and both models will represent the exact same state (with new changes syncing in seconds). When they are in perfect sync you can reverse the relationship.

## Step 3: Translate the Domain Model to the New Event Model

Instead of generating events according to the old model, change your entities to generate events according to the new structure. Do not deploy this until the new model is the main one. And deploy both those changes at the same time.

If you publish events according to the new model in the old model it will become incompatible with existing projections. Including the refactoring transformation! If you publish old events into the new model it will corrupt that one (which is less damning as it can be discarded and redone but adds unnecessary complication).

## Step 4: Test Everything

Yep: You should test. Test that the updated domain model generates the new events correctly. Test that the generated event model is a correct representation of the source event model. Test that the reverse transformation works well; you might want to test that applying the transformation followed by the reverse results in an unchanged event model.

## Step 5: Reverse the Roles

Now its time to make the new event model the main source and the old model a lowly projection only kept around for backward compatibility with existing projections.

Switch the command-strings in the server configuration and deploy the reverse translation instead of the forward one. To ensure that the models are in perfect sync, it might be a good idea to run the refactoring for an extra few minutes while prohibiting all external input (including commands and responding to events from other bounded contexts) before making the switch.

## Step 6: Update Projections

Incrementally, go through all your projections of this write-model and rewrite them to work with the new event model. Take them one at a time; it is not worth the risk of deploying them all at once. When one projection is deployed continue with the next.

If the position (and other metadata) of events have not changed (only their structure has) the projections will not need to restart from an empty database. They can just pick up from where they were. The events up to that point should represent exactly the same state changes in both models; they are just formatted and structured differently.

When all projections are deployed, you can move on to shelve the old event model. If you have external (to the core team) subscribers to the write-model you should inform them well ahead of the refactoring and once you've let them know of the successful deployment of the new model give them enough time to update their projections before shelving the event model.

> Note: You could start updating your projections well before step 5. And if you have finished all the updates, you might even dispense with the reverse translation altogether. That might however involve som added risk.
>
> It is recommended to focus on one problem at a time; first ensure that the translation works, then make sure that he domain changes are correct, and wait with projection until all that is assured.

## Step 7: Shelve the Old Event Model

Make sure that all dependent projections are updated. Then stop the reverse translation of the event models. Maybe wait a week to hear if stragglers are missing data. If you foresee no problems you can delete the old event model altogether.
