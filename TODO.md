# TODO

- Add readmes to the NuGet packages. <https://aka.ms/nuget/authoring-best-practices/readme>
- Command-line `dotnet test` fails sporadically (even habitually?)
  - It is always the `SQLServer*` tests that fail.
  - Maybe my Docker instance is slowed down sporadically?
- `SourceWebApplicationTests` deletes all data stored by `SourceWebApplication`.
  This data should however not be important so it might be okay.
- Explain somewhere that an *entity* is not a code object. It is the actual thing being modelled.
  The `Entity` object represents *a specific version* of the entity and it will not change that.
  When events are published, the entity's version is incremented, but the `Entity` remains as it was.
- `System.Diagnostics.Debugger.Break()` in `EventSource`.
- Rename MSSQL packages and APIs to SQLServer.
- No error when adding two command handlers with the same entry-point.
  - This should be handled by the `MapEndpoints` call, not by `Commanding`.
  - It may be possible to analyse all the commands and find discrepancies
    among them, but it is probably impossible to know if there are conflicting
    endpoints that were setup through other means.
- Can you add nuget.Development.config? Create a default muget.config committed to Git, and allow the
  developer to override some settings (e.g. source locations) in an untracked file?
- Convert file imports to a shared project import (Segerfeldt.EventStore.Shared.shproj).
  - Unfortunately this is difficult outside of Visual Studio proper.
    And VS For Mac has been discontinued, so a Windows machine may be needed.
