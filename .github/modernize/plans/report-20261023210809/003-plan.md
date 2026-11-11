# Task 003 execution plan

- Task: `003-transform-sql-managed-instance`
- Workspace: `C:\Users\ankitsurkar\Caldova-Asset-Manager`
- Branch supplied by caller: `modernize/dotnet-20261023211948`; no branch operations.
- Session: local fallback, 2026-10-23. No MCP session was issued: discovery returns names without callable tool definitions.
- Governing scope: confirmed `plan.md` and `.metadata/tasks.json` in this folder.
- Dependency blocker: task 001 has not ported System.Web/OWIN/Identity to .NET 10. Apply feasible task 003 work to the actual net472 / EF6.5.1 / Microsoft.Data.SqlClient5.2.3 runtime supplied by task 002; do not claim .NET 10 completion.

## Analysis and preservation

The application and ASP.NET Identity share `CaldovaAssetManagerContext`. Its current named connection is `DefaultConnectionString`. Startup registers `CreateDatabaseIfNotExists` with application/sample/Identity seed logic. No existing test project was found. Keep model mappings, query code, migrations, Identity tables/password hashes, transactions and controllers unchanged.

Preserve all prior/unrelated dirty work, including names, AssemblyInfo, publish profiles, scripts, README and docs. Do not run direct git commands; the required version-control MCP is not callable, so exact dirty inventory is unavailable. Do not switch/stash/commit.

## Ordered changes

1. Analysis: read current context, startup, provider config, initializer, provider evidence and confirmed requirements.
2. Dependencies: retain existing task-002 package versions/provider references; no new packages. Attempt Dotnet dependency guidance discovery; record unavailable tools.
3. Configuration: replace checked-in LocalDB connection with empty named Microsoft.Data.SqlClient connection. Remove LocalDB default factory. External process variable `SQLCONNSTR_DefaultConnectionString` overrides the same named connection; host-managed config injection may populate that connection instead. Empty/malformed/insecure settings fail closed without echoing connection strings.
4. Code: add `Utils/SqlDatabaseConfiguration.cs`, compile it in the existing csproj, and construct the shared context using its Microsoft SqlConnection with context ownership. Validate managed-identity auth (system assigned or user-assigned client ID), nonempty explicit TCP destination/database, encryption and certificate verification, no passwords/Windows auth/attached files. Disable EF initialization in the context's static constructor, remove seeder registration from Global, and validate configuration at startup without opening SQL. Keep seeder source unmodified but unreachable by runtime registration. Additional analysis found a separate `CreateAdminUser` startup bootstrap in `App_Start/Startup.Auth.cs`; remove its invocation and dead method (not ordinary registration/sign-in) to prevent startup role/admin/claim writes. Embedded admin config removal remains task 005; do not print its value.
5. Documentation: add `docs/SQL-MANAGED-INSTANCE.md` for actual config precedence, MI not SQL Database, identity/user permissions and least privilege, external schema/data migration with data/Identity preservation, networking/DNS/TLS/ports, developer constraints and acceptance gaps. No provisioning or deployment scripts.
6. Verification: attempt mandatory MCP build, CVE, consistency, completeness and test tools first if callable; otherwise local VS2019 MSBuild Debug/Release/solution builds, focused offline provider/configuration/model/query/initializer checks against built assemblies, static consistency/completeness checks, XML/metadata parsing and existing-test discovery. No new integration suite/baseline. No live Azure connections/data movement. Report absent unit tests and unavailable MCP/CVE checks rather than claiming passes.
7. Tracking: maintain this folder's `003-progress.md`, `003-summary.md` and focused check source. Update only task 003 metadata with honest execution detail; keep completion pending while upstream/runtime/live acceptance is blocked. Do not rewrite other tasks.

## Exact intended file scope

- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/SqlDatabaseConfiguration.cs` (new)
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Models/CaldovaAssetManagerContext.cs`
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Global.asax.cs`
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/App_Start/Startup.Auth.cs` (remove startup bootstrap only)
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config`
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj` (one Compile item)
- `docs/SQL-MANAGED-INSTANCE.md` (new)
- This folder: `003-plan.md`, `003-progress.md`, `003-summary.md`, `003-VerifyManagedInstance.cs`, `003-VerifyStructure.py` (repeatable local structural evidence) and `.metadata/tasks.json` (task 003 execution evidence only).
