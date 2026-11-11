# Task 003 — SQL Managed Instance implementation evidence

## Result and status

Implemented feasible **Azure SQL Managed Instance**, not SQL Database, runtime
work on the actual **net472 / System.Web / OWIN / ASP.NET Identity / EF6.5.1**
application. Retained Microsoft.Data.SqlClient5.2.3 and
Microsoft.EntityFramework.SqlServer6.5.1 from task 002; no packages added.

**Overall acceptance remains pending/blocked**, not complete: task 001 has not
ported the application to .NET 10/ASP.NET Core. Installing SDK10 or retargeting
this legacy web project is not that port. Task 003's metadata `status` remains
`pending`, with a task-specific description of implemented work and blockers.
Tasks 001/002/004–009 and dependency ordering were not changed.

No resource provisioning, deployment, container work, new integration suite,
baseline capture, Entra end-user sign-in changes or live data migration occurred.
No unit-test, CVE or live-cloud pass is claimed.

## Changes

- Shared application/Identity connection retains logical name
  `DefaultConnectionString`. Host process variable
  `SQLCONNSTR_DefaultConnectionString` overrides externally injected named
  configuration; the checked-in named value is empty, with no LocalDB fallback.
- Configuration rejects missing/malformed settings, non-TCP/missing endpoints or
  missing databases, non-managed-identity auth, SQL passwords, integrated auth,
  disabled encryption, certificate bypass, non-GUID user IDs, attached database
  files, user instances and persistent security info. User-assigned identity
  uses its **client ID**; system-assigned identity omits it.
- Startup validates settings without opening SQL or acquiring a token.
  Diagnostics do not echo supplied connection values or parser inner exceptions.
- Actual shared IdentityDbContext now owns the explicitly constructed
  Microsoft.Data.SqlClient connection. Model mappings, queries, migrations,
  transaction boundaries, Identity data types/hashes and normal user-sign-in/
  registration behavior are unchanged.
- The context static constructor disables EF initialization, including outside
  HTTP startup. Removed Global's sample-initializer registration and OWIN's
  startup administrator/role/claim bootstrap. The historical sample initializer
  source remains unregistered; ordinary application data writes still work.
- New operator documentation separates least-privilege SQL runtime access from
  controlled external schema/data deployment, and covers MI networking/DNS/
  ports/certificates, identity mapping, Identity/data preservation and rollback.

## Exact manually changed/added files

Paths relative to workspace `C:\Users\ankitsurkar\Caldova-Asset-Manager`:

1. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Utils\SqlDatabaseConfiguration.cs` — added.
2. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Models\CaldovaAssetManagerContext.cs` — shared connection and initializer policy only.
3. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Global.asax.cs` — replace seeder registration with configuration validation.
4. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\App_Start\Startup.Auth.cs` — remove startup-only admin bootstrap and its now-unused imports.
5. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config` — empty named external connection, remove LocalDB factory; other settings retained.
6. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj` — one Compile item only.
7. `docs\SQL-MANAGED-INSTANCE.md` — added.
8. `.github\modernize\plans\report-20261023210809\003-plan.md` — added.
9. `.github\modernize\plans\report-20261023210809\003-progress.md` — added.
10. `.github\modernize\plans\report-20261023210809\003-summary.md` — added.
11. `.github\modernize\plans\report-20261023210809\003-VerifyManagedInstance.cs` — added offline evidence, not a new integration suite.
12. `.github\modernize\plans\report-20261023210809\003-VerifyStructure.py` — added repeatable local structural evidence.
13. `.github\modernize\plans\report-20261023210809\.metadata\tasks.json` — task 003 description/evidence only; status honestly stays pending.

Builds also generate normal `bin`/`obj` outputs and the offline executable in
website `bin`; those are not source changes. No edits were made to prior task
artifacts, confirmed `plan.md`, solution/project names, AssemblyInfo, publish
profiles, existing scripts, README, existing docs, package declarations, EF
migrations or SQL project. Exact whole-working-tree dirty inventory cannot be
attested: version-control MCP was not callable and direct git was not used.
Supplied branch: `modernize/dotnet-20261023211948`. No branch change, stash or commit.

## Exact validation commands and outcomes

All commands ran locally from the workspace root, using the already-installed
VS2019 BuildTools and restored packages. No restore/package update required.

### Builds

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Debug /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Release /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.sln /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
```

All **exit 0**. Release emitted existing `ManageController.cs(135,41)` warning
CS1998 (async method lacks await), unrelated to SQL. Solution build emitted
website output; no database deployment was requested or performed.

### Focused actual-assembly/configuration checks

Ran this compile/run command immediately after Debug Rebuild, and again
immediately after Release Rebuild:

```powershell
$bin=Join-Path (Get-Location) 'IaaS2PaaSWeb\Caldova-Asset-Manager.Website\bin'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$bin\ManagedInstanceCheck.exe" /reference:System.Data.dll /reference:System.Configuration.dll /reference:System.Xml.Linq.dll /reference:System.ComponentModel.DataAnnotations.dll "/reference:$bin\EntityFramework.dll" "/reference:$bin\Microsoft.EntityFramework.SqlServer.dll" "/reference:$bin\Microsoft.Data.SqlClient.dll" "/reference:$bin\Microsoft.AspNet.Identity.EntityFramework.dll" "/reference:$bin\Microsoft.AspNet.Identity.Core.dll" "/reference:$bin\Caldova-Asset-Manager.dll" .github\modernize\plans\report-20261023210809\003-VerifyManagedInstance.cs
if ($LASTEXITCODE -eq 0) { & "$bin\ManagedInstanceCheck.exe" IaaS2PaaSWeb\Caldova-Asset-Manager.Website; exit $LASTEXITCODE } else { exit $LASTEXITCODE }
```

Both runs **exit 0, 44 assertions passed each**:

- Checked-in provider/name/empty destination; missing, blank, malformed and invalid
  auth settings rejected with sanitized errors.
- Encryption/certificate enforcement, Windows/SQL/default-credential-chain/
  integrated-Entra auth rejection, required server/database, no LocalDB/non-TCP,
  invalid user client ID, file attachment, user instance or persistent security info.
- System-assigned and user-assigned managed identity, Mandatory and Strict TLS
  policy; validation never opens the connection.
- Actual registered ADO.NET factory/EF6 provider, actual compiled shared context
  using the configured database, unchanged lazy loading.
- Actual EF context initializer is disabled and its invocation does not open SQL.
- Product/Identity model and generated `[dbo].[Products]` /
  `[dbo].[AspNetUsers]` SQL translation without opening SQL.
- Compiled admin bootstrap absent, sample initializer not registered, LocalDB
  default factory absent, x86/x64 native SNI outputs present.
- Named-config fallback and process-variable precedence verified in isolated
  AppDomains using temporary copies of Web.config; blank override cannot fall back
  to valid named configuration; wrong named provider rejected. Temporary config
  deleted, original environment restored. No real endpoint or credential used.

### Structural consistency/completeness and existing tests

```powershell
python .github\modernize\plans\report-20261023210809\003-VerifyStructure.py
Get-ChildItem IaaS2PaaSWeb -Recurse -Include *.csproj,*.vbproj,*.fsproj | Where-Object { $_.FullName -notmatch '\\packages\\' } | Select-Object -ExpandProperty FullName
```

Both **exit 0**. Structural checks confirm 9 unique ordered task IDs/dependencies,
honest pending task-003 status, valid XML, all source/HintPaths and 80 package
folders present, unique Compile item, both startup bootstraps removed, no active
legacy SqlClient or runtime database-create/automatic-migration calls, current
v4.7.2 target, required documentation and no existing test project.
The sole code project is the website; **no existing unit tests were available to
run**, and no unit-test pass is asserted. Known task-schema `allOf` bug excluded;
no schema tooling or unrelated schema changes introduced.

First inline structural probe failed on an existing UTF-16 C# source while
assuming UTF-8. Fixed the **probe**, not the application source, to detect UTF-16
BOMs; persisted script reran successfully. No application build/harness failures.

## Unavailable tool checks and outstanding prerequisites

Modernization tool discovery returned matching tool **names**, but did not expose
callable definitions. Therefore runner/session initialization, markdown preview,
version-control status/setup, Dotnet dependency KB, MCP build/CVE/test and
consistency/completeness validation could not be invoked. Local build and
focused/structural checks above replace the feasible checks, **not** MCP success.
No CVE scan performed; no vulnerability-free claim.

No live SQL connection or managed-identity token acquisition, SQL permission
test, network/DNS/TLS handshake, migration, transaction execution or cloud
deployment was performed. These require an externally selected MI, database,
host managed identity/SQL user and grants, network path and controlled schema/
data migration. These prerequisites and external acceptance procedures are in
`docs\SQL-MANAGED-INSTANCE.md`.

Existing administrator credential configuration is now unused by startup;
credential removal/rotation belongs to task 005 and remains outstanding.
Historical seeder code is intentionally retained but unregistered.

This is a completed feasible local MI change, **not** successful completion of
all modernization tasks. No final commit or branch operation performed.
