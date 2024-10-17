# TODO

## Icebox Issues & Ideas

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
- Report VSCode bug.
    When running single test in the Testing view, VSCode builds with the wrong command:

    ```sh
    dotnet build /project_path/project_name.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Debug /p:Platform="AnyCPU"
    ```

    I have the `$(SolutionDir)` prefix in my project reference paths; I want to be free to change the folder structure without having to adjust all the paths. But the above command doesn't know about the solution, so the `$(SolutionDir)` value is empty and build fails. This command however works (assuming all dots in the target name are replaced with underscores):

    ```sh
    dotnet build /solution_path/Solution.sln /target:rel_project_path/project_name /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Debug /p:Platform="Any CPU"
    ```
