# TODO

- Add a `GET` endpoint that queries for new events after a specified `Position`.
  - Probably implemented in the *Source* package.
    - That's where `Commanding` and its endpoints (including `history`) are already implemented.
    - *Projection* on the other hand already defines `Event` and the `EventSource` emitter.
  - But it should be filtered somehow. Make some events (and entire entities) internal and thus not visible.
  - Full-access projections (read-model in the same BC) can still use direct-from-database Projection.

## Icebox Issues & Ideas

- Add more user manual stuff
  - How to define `[ReceivesEvent(eventName, Entity = typeName)]` methods
  - How to define `[ReplaysEvent(eventName)]` methods
- Integrate better with the authentication handler.
  - `Forbid()` in a controller calls `Forbid()` on the handler.
  - The `AuthorizeAttribute` triggers the authorization handler(s) before even instantiating the controller. If the auth handler doesn't accept the user credentials the endpoint is not executed.
  - How do auth-schemes work? Should the auth scheme be detected and used to select which handler(s) to use?
  - Is it because of the support for multiple handlers (that could all accept) that the `IPrincipal` has multiple `Identities`?
  - What else?
  - But how do authentication schemes work??
- Add more tests for commanding and hosting.
- Add tests for documentation.
- There are almost no tests at all for Refactoring.
- Command-line `dotnet test` fails sporadically.
- `SourceWebApplicationTests` deletes all data stored by `SourceWebApplication`.
  This data should however not be important so it might be okay.
- Rename MSSQL packages to SQLServer.
- No error when adding two command handlers with the same entry-point.
  - This should be handled by the `MapEndpoints` call, not by `Commanding`.
  - It may be possible to analyse all the commands and find discrepancies
    among them, but it is probably impossible to know if there are conflicting
    endpoints that were setup through other means.
