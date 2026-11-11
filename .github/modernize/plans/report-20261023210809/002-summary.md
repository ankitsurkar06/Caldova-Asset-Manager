# Task 002: SQL client implementation evidence

## Outcome

**Independent implementation completed and locally verified; upstream runtime dependency remains blocked.** Task 001 made no code port. The website remains System.Web/MVC5/OWIN/Identity, now targeting **.NET Framework 4.7.2** solely to support the new SQL provider. This is not the confirmed .NET 10 upgrade and does not complete task 001. Task metadata remains unchanged/pending; no schema `allOf` validation was used as a blocker.

Workspace: `C:\Users\ankitsurkar\Caldova-Asset-Manager`.
Branch supplied by caller and consistent with read-only `.git\HEAD`: `modernize/dotnet-20261023211948`.
No migration-session ID was generated: discovery returned MCP names but no callable definitions.

## Changed application files

1. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj`
   - Transitional target v4.7.2; existing assembly name, root namespace and project identity preserved.
   - EF6.5.1 plus `Microsoft.EntityFramework.SqlServer` 6.5.1 and `Microsoft.Data.SqlClient` 5.2.3. Removed the old `EntityFramework.SqlServer` reference.
   - Explicit transitive runtime references for packages.config and native SNI 5.2.0 target import with a missing-restore build error.
2. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\packages.config`
   - Updated EF and added compatible provider/driver and their resolved dependency closure. Unrelated package versions retained.
3. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config`
   - Kept `DefaultConnectionString` and its destination/integrated-security values; changed its provider to Microsoft.Data.SqlClient.
   - Replaced EF services and default LocalDB factory; registered the corresponding ADO.NET factory.
   - Explicit `Encrypt=False` applies to the saved **local development LocalDB** string only, preserving legacy behavior despite the new driver's changed default. Production external connections must use `Encrypt=True` and valid certificate validation; no production endpoint was configured here.
   - Framework settings aligned; runtime binding redirects updated/added from actual build conflict diagnostics. No credentials edited or reproduced in this report.

Added evidence files beside this report: `002-plan.md`, `002-progress.md`, `002-VerifySqlProvider.cs`, `002-summary.md`.
Package downloads and build outputs are in already-ignored `packages`, `bin` and `obj` directories. Repository `nuget.exe` was not replaced.

## Preservation

No edits were made to query/controller/model/context code, transaction code, schema, initializer, Identity mappings or sign-in behavior. `CaldovaAssetManagerContext` still uses `base("name=DefaultConnectionString")` and disables lazy loading. No EF Core conversion or schema/data migration was attempted.

Existing solution/project renames, AssemblyInfo, publish profiles, custom scripts (including `vsDevWorkstationConfig.ps1`), README, docs, confirmed `plan.md`, `.metadata\tasks.json` and task 001 evidence were not overwritten. Only the three listed application files and four new task-002 artifacts were written manually, all via apply_patch.

An authoritative initial/final dirty-file inventory **could not be obtained**: the discovered version-control tool had no callable definition. No direct Git command was executed. Read-only `.git\HEAD` inspection confirms the expected branch name, not the exact dirty state. Therefore preservation is stated as the actual limited write scope, not a certified Git diff.

## Commands and results

All commands below ran from the workspace. Variables shown were set within the same PowerShell invocation where used.

### Tooling and package acquisition

- `dotnet --info`: SDK 10.0.402, MSBuild 18.9.15. No claim that the website uses that runtime.
- Read the confirmed plan and its nested `.metadata\tasks.json` in full. The initially attempted root `.metadata\tasks.json` did not exist; the linked nested task file was then read.
- `.\IaaS2PaaSWeb\nuget.exe install Microsoft.EntityFramework.SqlServer -Version 6.5.1 -OutputDirectory IaaS2PaaSWeb\packages -Source https://api.nuget.org/v3/index.json -NonInteractive`
- `.\IaaS2PaaSWeb\nuget.exe install Microsoft.Data.SqlClient -Version 5.2.3 -OutputDirectory IaaS2PaaSWeb\packages -Source https://api.nuget.org/v3/index.json -Framework net472 -NonInteractive`
  - Both failed TLS negotiation. A temporary NuGet 6.14 retry against the same endpoint also failed. Direct PowerShell access to the v3 flat-container endpoint failed handshake negotiation too.
- `Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/v6.14.0/nuget.exe -OutFile "$env:TEMP\appmod-nuget-6.14.exe"`: succeeded; only temporary tool changed.
- `& "$env:TEMP\appmod-nuget-6.14.exe" install Microsoft.Data.SqlClient -Version 5.2.3 -Framework net472 -OutputDirectory IaaS2PaaSWeb\packages -Source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json -NonInteractive`: succeeded, including dependencies.
  - The corresponding provider install against that mirror alone failed because provider 6.5.1 was absent there.
- `Invoke-WebRequest https://www.nuget.org/api/v2/package/Microsoft.EntityFramework.SqlServer/6.5.1 -OutFile "$env:TEMP\Microsoft.EntityFramework.SqlServer.6.5.1.nupkg"`: succeeded.
- `& "$env:TEMP\appmod-nuget-6.14.exe" install Microsoft.EntityFramework.SqlServer -Version 6.5.1 -Framework net472 -OutputDirectory IaaS2PaaSWeb\packages -Source "https://www.nuget.org/api/v2/;https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" -NonInteractive`: exit 0. Inspected actual package nuspecs and assets. The provider requires EF6.5.1 and SqlClient >=5.1.5 on net462; selected SqlClient5.2.3 satisfies this. Older alternate downloaded versions are not referenced.
- `& "$env:TEMP\appmod-nuget-6.14.exe" restore IaaS2PaaSWeb\Caldova-Asset-Manager.Website\packages.config -PackagesDirectory IaaS2PaaSWeb\packages -Source "https://www.nuget.org/api/v2/;https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" -NonInteractive`: exit 0, all declared packages installed.

These are package downloads only; no private source or credentials were uploaded. No feed change was persisted.

### Builds

Build executable:
`C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe`

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
```

Before edits: exit 0.

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Debug /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Release /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.sln /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
```

All exited 0. Initial changed Debug rebuild reported MSB3247 binding conflicts, resolved by explicit Web.config redirects; repeat Debug rebuild passed without those warnings. Release reported CS1998 in unchanged `Controllers\ManageController.cs(135,41)` (async method without await); no unrelated fix applied. The current solution contains the website and Environments project, **not** the database sqlproj; solution-build success is not SQL database-project validation, provisioning or deployment.

### Focused offline checks

```powershell
$bin = (Resolve-Path IaaS2PaaSWeb\Caldova-Asset-Manager.Website\bin).Path
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$bin\SqlProviderCheck.exe" /reference:System.Data.dll /reference:System.Configuration.dll /reference:System.ComponentModel.DataAnnotations.dll "/reference:$bin\EntityFramework.dll" "/reference:$bin\Microsoft.EntityFramework.SqlServer.dll" "/reference:$bin\Microsoft.Data.SqlClient.dll" "/reference:$bin\Microsoft.AspNet.Identity.EntityFramework.dll" "/reference:$bin\Microsoft.AspNet.Identity.Core.dll" "/reference:$bin\Caldova-Asset-Manager.dll" .github\modernize\plans\report-20261023210809\002-VerifySqlProvider.cs
& "$bin\SqlProviderCheck.exe" IaaS2PaaSWeb\Caldova-Asset-Manager.Website
```

Compilation and execution exited 0. Repeated against Release and final Debug output: exit 0.

The persisted source is an offline assertion helper, not a new integration suite. It loads the **actual Web.config** in a separate Framework AppDomain, resolves the named connection and both factories, instantiates the real context, builds its real model with an explicit SQL2012 manifest token without database discovery, and translates product and Identity queries. All 15 assertions passed:

- Named provider, ADO.NET factory, EF provider services, Microsoft LocalDB factory and default Microsoft connection.
- Closed real-context Microsoft connection, preserved server/database/integrated security and saved LocalDB destination.
- Disabled lazy loading; product and Identity model inclusion.
- Product SQL targets `[dbo].[Products]`; Identity SQL targets `[dbo].[AspNetUsers]`.
- Connection stays closed throughout.
- Native SNI x86/x64 DLLs present (arm64 also copied).

Additional local checks: all **80** packages.config entries and all project HintPaths resolve; no active old SQL client/provider reference remains; project/packages/Web.config parse as XML. A scoped source search found no System.Data.SqlClient or old EF6.1.3 active references. These checks do not prove live transaction execution or full application behavior.

## Remaining blockers and exclusions

- Task 001 / .NET 10 / ASP.NET Core compatibility remains undone. Later runtime work must deliberately retain/reconcile these EF6 provider settings, mappings and named connection semantics; do not treat this Framework retarget as a runtime port.
- MCP discovery returned tool names without definitions, so runner initialization, version-control status, MCP build/test/CVE checks and markdown preview could not be invoked. Local implementation and verification were performed instead; no MCP success is claimed.
- No existing test csproj was found (the website is the sole csproj). No unit-test run/pass is claimed.
- No live SQL, Azure, Identity sign-in, transaction execution or controlled schema/data migration verification. Preserve/validate real-service behavior externally after upstream compatibility work.
- No CVE scan or vulnerability-free claim. No resource provisioning, deployment, containerization, baseline task, integration suite or Entra user-sign-in changes.
- No branch creation/switch, stash or commit performed; upstream blockers prevent claiming all migration work successfully completed.
