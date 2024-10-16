# TODO

- Command-line `dotnet test` fails sporadically (even habitually?)
- `SourceWebApplicationTests` deletes all data stored by `SourceWebApplication`.
  This data should however not be important so it might be okay.
- Explain somewhere that an *entity* is not a code object. It is the actual thing being modelled.
  The `IEntity` object represents *a specific version* of the entity and it will not change that.
  When events are published, the entity's version is incremented, but the `IEntity` remains as it was.
- Rename MSSQL packages to SQLServer.
- No error when adding two command handlers with the same entry-point.
  - This should be handled by the `MapEndpoints` call, not by `Commanding`.
  - It may be possible to analyse all the commands and find discrepancies
    among them, but it is probably impossible to know if there are conflicting
    endpoints that were setup through other means.
- Convert file imports to a shared project import (Segerfeldt.EventStore.Shared.shproj).
  - Unfortunately this is difficult outside of Visual Studio proper.
    And VS For Mac has been discontinued, so a Windows machine may be needed.
- Report VSCode bug.
    When running single test in the Testing view, VSCode builds with the wrong command:

    ```sh
    dotnet build /Users/johan/Arbete/Segerfeldt/Segerfeldt.EventStore/Tests/PostgreSQL/Segerfeldt.EventStore.Projection.PostgreSQL.Tests/Segerfeldt.EventStore.Projection.PostgreSQL.Tests.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Debug /p:Platform="AnyCPU"
    ```

    I have the `$(SolutionDir)` prefix in my project reference paths; I want to be free to change the folder structure without having to adjust all the paths. But the above command doesn't know about the solution, so the `$(SolutionDir)` value is empty and build fails. This command however works:

    ```sh
    dotnet build /Users/johan/Arbete/Segerfeldt/Segerfeldt.EventStore/Segerfeldt.EventStore.sln /target:PostgreSQL\Segerfeldt_EventStore_Projection_PostgreSQL /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Debug /p:Platform="Any CPU"
    ```
