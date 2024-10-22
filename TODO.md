# TODO

## Icebox Issues & Ideas

- Add more tests for commanding and hosting.
- Add tests for documentatioon.
- Maybe `Reconstitute` should accept a `string` instead of an `EntityId`?
  - If you have stored “invalid” entities, you should still be able to reconstitute them.
  - Could just make `EntityId.Safe()` public, but that might cause developers to bypass validation where it shouldn't be.
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
