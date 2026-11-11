# Task 002 implementation plan

- Task: `002-transform-sqlclient`; workspace: `C:\Users\ankitsurkar\Caldova-Asset-Manager`.
- Branch supplied by caller: `modernize/dotnet-20261023211948`; no branch operations.
- Migration session: unavailable. Discovery returned tool names without callable MCP definitions; task-runner initialization and version-control status are blocked.
- Authority: confirmed `plan.md` and `.metadata/tasks.json`, read in full. This is execution detail, not a replacement plan.

## Analysis and compatibility

Task 001 made no runtime port. The current application is System.Web/MVC/Identity on .NET Framework 4.5.1 with EF 6.1.3, not .NET 10. Keep EF6/Identity mappings and context behavior; do not substitute EF Core. Microsoft's EF6 Microsoft.Data.SqlClient provider requires a newer Framework. Apply only the transitional Framework retarget required for this driver (4.7.2, installed reference assemblies), not an ASP.NET Core migration or a claim that task 001 is complete.

## Changes

1. Align EF6, Microsoft.EntityFramework.SqlServer and Microsoft.Data.SqlClient packages, their transitive dependencies, project assembly references and native SNI build targets using the existing packages.config layout. Inspect actual package metadata before selecting reference paths.
2. Retarget the project and Web.config Framework settings to 4.7.2 for package compatibility.
3. Change the existing `DefaultConnectionString` provider, EF provider services, default LocalDB connection factory and ADO.NET provider registration together. Keep database/server/integrated-security values and named lookup unchanged. Preserve local development encryption behavior explicitly rather than silently depending on the new driver's default; no production TLS downgrade.
4. Do not change queries, models, Identity mapping, initializer, transactions, routes, startup, solution name, assembly identity, publish profiles, scripts, README, docs or other tasks.
5. Save status and evidence here; leave task dependency 001 blocked, never assert .NET 10 or real SQL success.

## Verification

Discover mandatory build/test/consistency/completeness/CVE and dependency-guidance MCP tools; if not callable record the limitation. Use local focused builds, provider/configuration resolution and model/query checks without opening a SQL connection. Find applicable existing tests; do not invent an integration suite or report absent tests as passing. Record build prerequisites and any pre-existing failures accurately. No cloud operations.

## Preservation

Version-control status cannot be obtained through the discovered tools. No direct Git commands are allowed. Inspect branch metadata without changing it; preserve all out-of-scope files by not writing them. No baseline of unrelated dirt can be certified.
