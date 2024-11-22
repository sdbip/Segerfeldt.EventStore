# TODO

- `MockProjectionTracker` is duplicated many times

## Icebox Issues & Ideas

- Add more user manual stuff
  - How to define `[ReceivesEvent(eventName, Entity = typeName)]` methods
  - How to define `[ReplaysEvent(eventName)]` methods
  - How to set up the new `WebServiceEventSourceRepository` and endpoint.
- Integrate better with the authentication handler.
  - `Forbid()` in a controller calls `Forbid()` on the handler.
  - The `AuthorizeAttribute` triggers the authorization handler(s) before even instantiating the controller. If the auth handler doesn't accept the user credentials the endpoint is not executed.
  - How do auth-schemes work? Should the auth scheme be detected and used to select which handler(s) to use?
  - Is it because of the support for multiple handlers (that could all accept) that the `IPrincipal` has multiple `Identities`?
  - Or is it only because there can be multiple `Authorization` headers?
  - What else?
  - But how do authentication schemes work?? My `NaiveAuthenticationhandler` is added with an empty scheme name. It doesn't seem to work otherwise. Are only some strings acceptable? Does the string have to match something in the request structure? Or something that is configured elsewhere?
- Add more tests for commanding and hosting.
- Add tests for documentation.
- There are almost no tests at all for Refactoring.
- Command-line `dotnet test` fails sporadically.
- Rename MSSQL packages to SQLServer.
- No error when adding two command handlers with the same entry-point.
  - This should be handled by the `MapEndpoints` call, not by `Commanding`.
  - It may be possible to analyse all the commands and find discrepancies
    among them, but it is probably impossible to know if there are conflicting
    endpoints that were setup through other means.
- Add sample applications using all database implementations
- `Position` and `EventOrdinal` should not have a `[JsonConverter]`
  - `EventDAO` prevents removing it though.
- Rename `EventOrdinal` and merge with `EntityVersion`
