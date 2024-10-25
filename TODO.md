# TODO

## Icebox Issues & Ideas

- Integrate better with the authentication handler.
  - `Forbid()` in a controller calls `Forbid()` on the handler.
  - The `AuthorizeAttribute` triggers the authorization handler(s) before even instantiating the controller. If the auth handler doesn't accept the user credentials the endpoint is not executed.
  - How do auth-schemes work? Should the auth scheme be detected and used to select which handler(s) to use?
  - Is it because of the support for multiple handlers (that could all accept) that the `IPrincipal` has multiple `Identities`?
  - What else?
- Automate the `actor` and create methods (possibly extension-methods) that publish changes.
- Create a refactoring function/tool. Probably in a new package?
  - Go through all events just like a projection,
  - But for each event, run the details through some sort of translation mechanism that generates new events
  - Maybe split the event in two or more.
  - Keep the same timestamp, position and actor,
  - Allow changes to the details, name and possibly even the entity.
  - Ordinal seems complex.
  - Allow it to run in parallel with the existing model.
  - Keep projecting the old model into the new model for a while.
  - When the new model is caught up with the state, make it the main model.
  - Perhaps project the new model into the old so that other projections have some time to make the switch.
  - When all projections are switched to the new model, remove the old altogether.
- Answer this: Can you run code synchronously yet have Tasks that are completed when they return? `Task.FromResult()`?
  - Could be a problem if synchronous code in receptacles is actually run in parallel.
- Add more tests for commanding and hosting.
- Add tests for documentation.
- Command-line `dotnet test` fails sporadically.
- `SourceWebApplicationTests` deletes all data stored by `SourceWebApplication`.
  This data should however not be important so it might be okay.
- Explain somewhere that an *entity* is not a code object. It is the actual thing being modelled.
  The `IEntity` object represents *a specific version* of the entity and will not change by publishing.
  When events are published, the entity's version is incremented, but the `IEntity` remains as it was.
- Should it be possible to publish multiple times with the same `IEntity`?
- Rename MSSQL packages to SQLServer.
- No error when adding two command handlers with the same entry-point.
  - This should be handled by the `MapEndpoints` call, not by `Commanding`.
  - It may be possible to analyse all the commands and find discrepancies
    among them, but it is probably impossible to know if there are conflicting
    endpoints that were setup through other means.
- Convert file imports to a shared project import (Segerfeldt.EventStore.Shared.shproj).
  - Unfortunately this is difficult outside of Visual Studio proper.
    And VS For Mac has been discontinued, so a Windows machine may be needed.
    (Or maybe JetBrains Rider has the ability?)
