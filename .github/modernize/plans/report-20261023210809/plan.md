# Modernization Plan: Caldova Asset Manager

**Project**: Caldova-Asset-Manager  
**Assessment**: [report-20261023210809](../../assessment/reports/report-20261023210809/report.json)  
**Language**: .NET / C#  
**Status**: Webapp-only implementation in progress; build and local checks pass.

## Execution Scope Revision

The user subsequently authorized code modernization, excluded SQL Managed
Instance work, and explicitly confirmed continuing the .NET 10 / ASP.NET Core
port. This revision supersedes the original Azure-service task scope below.

- The active work is the website runtime/framework port and necessary webapp
  compatibility changes. No database project or SQL script is changed.
- Separate SQL MI, Blob, Key Vault and Redis migrations are not being executed.
  Earlier SQL MI-specific helper files remain untouched historical source and
  are excluded from the modern web project.
- Existing SQL configuration is supplied externally and is not restricted to
  SQL MI or managed identity. No database is created, seeded or migrated.
- The SDK-style net10.0 webapp compiles, including Razor views. Local static
  assets, authorization redirects and SignalR negotiation have been verified.
  Offline checks validate the existing schema mappings and Identity 2 hashes.
- Live database-backed user journeys remain unverified; no local SQL instance
  was available. The build is not a certification of production readiness.
- See [README](../../../../README.md) for publish commands, configuration and
  outstanding environment prerequisites. No resources were provisioned and
  no application was deployed.

The remaining sections preserve the originally confirmed assessment plan as
historical context. Current execution disposition is recorded in
[tasks.json](.metadata/tasks.json).

## Technical Framework

- **Runtime**: .NET Framework 4.5.1.
- **Framework**: ASP.NET MVC 5 / Web API, System.Web, OWIN and ASP.NET Identity.
- **Build**: Legacy MSBuild web project, packages.config and SQL database project.
- **Database**: SQL Server LocalDB through Entity Framework 6.1.3.
- **Dependencies**: Classic SignalR 2.2.2, System.Data.SqlClient and MemoryCache.
- **Source**: [Website project](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj).
- **Target runtime**: .NET 10 LTS, with the ASP.NET Core compatibility changes
  needed to replace the unsupported framework.

## Overview

Modernize the assessed application for Azure service integration while preserving
its existing storefront, administration, authentication and recommendation
behavior. The migration will:

- Replace the unsupported runtime and retain existing routes and user data.
- Move database access to Azure SQL Managed Instance using managed identity.
- Serve the identified static content from Azure Blob Storage.
- Remove actual embedded credentials and protect remaining secrets in Key Vault.
- Preserve real-time announcements on the modernized runtime.
- Replace process-local product caching with shared Azure Redis caching.
- Externalize the executable recommendation endpoint and modernize HTTP access.

Runtime compatibility comes first. Subsequent code transformations run
sequentially to avoid conflicting edits. The machine-readable task breakdown,
requirements, execution guidance and dependencies are in
[tasks.json](.metadata/tasks.json).

No Azure resources will be provisioned, no application will be deployed, and no
container image will be created. Separate baseline capture, integration testing
and dependency CVE remediation are excluded at the user's explicit request.

## Migration Impact Summary

| Area | Original | Target | Authentication |
|------|----------|--------|----------------|
| Runtime | .NET Framework 4.5.1 | .NET 10 LTS | Existing sign-in |
| SQL | LocalDB / SQL Server | Azure SQL MI | Managed identity |
| Driver | System.Data.SqlClient | Microsoft.Data.SqlClient | SQL identity |
| Assets | Website static files | Azure Blob Storage | Managed identity |
| Secrets | Embedded/config values | Azure Key Vault | Managed identity |
| Real time | Classic SignalR | Compatible modern hubs | Existing policy |
| Cache | MemoryCache | Azure Redis caching | Managed identity |
| Settings | Embedded endpoint URL | External configuration | Host managed |
| HTTP | Legacy HTTP wrapper | Supported HTTP access | Existing contract |

Blob management uses managed identity. Browser asset delivery must preserve
current visibility without exposing storage credentials or granting unintended
public access. Runtime hosting remains undecided because deployment is excluded.

## Assessment Verification and Scope

The supplied report contains 11 issue types and 66 incidents. These are scanner
findings, not 66 independently confirmed migration requirements. Paths in the
report are relative to the solution directory,
[IaaS2PaaSWeb](../../../../IaaS2PaaSWeb).

| Finding | Current-source verification | Disposition |
|---------|-----------------------------|-------------|
| Old .NET Framework dependency detected | Website project targets v4.5.1. | Upgrade to .NET 10 LTS. |
| Static content detected | Images, CSS, scripts and fonts are packaged locally and referenced by views. | Migrate identified assets to Blob Storage; do not move executable views or source files. |
| SQL database connection detected | Web.config uses LocalDB and integrated security; CaldovaAssetManagerContext uses DefaultConnectionString. | Target SQL Managed Instance, overriding the saved SQL Database selection. |
| System.Data.SqlClient dependency detected | Connection and EF provider configuration reference System.Data.SqlClient. | Migrate driver and compatible ORM provider together. |
| Windows authentication detected | No Windows user-authentication configuration was found; startup uses Identity cookies/social providers. Windows integrated security is present on the SQL connection. | Exclude standalone Windows AD to Entra user-sign-in migration; database identity migration remains included. |
| Connection strings without configuration builders detected | Database configuration is local to Web.config. | Externalize configuration and use managed identity; do not mechanically add legacy builders after the runtime migration. |
| Hardcoded sensitive data detected | Administrator password configuration is populated. Other hits include empty optional provider secrets and ordinary password-related model/controller code. | Remove actual embedded credentials; do not classify every scanner hit as a secret. |
| Potential duplex communication is detected | AnnouncementHub and administrator broadcasts are registered through classic SignalR. Some browser script registration is commented out. | Preserve actual announcement behavior and authorization without activating disabled UI features. |
| Data caching is detected | Home, store, admin and layout paths use MemoryCache. | Shared caching with existing expiry and invalidation semantics. |
| Hardcoded URLs detected | Recommendation engine constructs an executable remote scoring URL. Other URLs occur in comments. | Externalize executable configuration only. |
| Access to external resources via HTTP is detected | HttpClientWrapper supplies recommendation authentication. | Preserve the contract and verify error handling without silently claiming endpoint availability. |

Evidence includes
[Web.config](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config),
[authentication startup](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/App_Start/Startup.Auth.cs),
[database context](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Models/CaldovaAssetManagerContext.cs),
[recommendation engine](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Recommendations/AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs),
[HTTP wrapper](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/HttpClientWrapper.cs),
[announcement hub](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Hubs/AnnouncementHub.cs),
[store controller](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Controllers/StoreController.cs)
and [layout data](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/LayoutDataAttribute.cs).
No credential values are reproduced in these deliverables.

## Validation and External Prerequisites

- Every code task requires a successful build and applicable existing tests.
  Use focused local checks of the changed behavior; no new integration suite or
  baseline task is authorized. Do not report absent tests as passing.
- Validate image/script/style references, SQL provider compatibility, cache
  expiry/invalidation, user identity and announcement payload behavior.
- Azure resource endpoints, managed identities, RBAC, SQL database users,
  network access, asset transfer and controlled schema/data migration must be
  supplied or performed externally before real-service acceptance.
- Preserve database records, identity identifiers/password hashes, cart and
  authorization semantics during the runtime migration. Do not assume a simple
  target-framework edit can replace System.Web, OWIN or classic Identity.
- The runtime task owns framework compatibility, including required SignalR
  server/client porting. The later real-time task owns announcement-specific
  behavior checks and any remaining migration adjustments.
- Review the legacy recommendation endpoint's availability during execution.
  If unavailable, report the blocker and request a replacement contract; do not
  invent a new recommendation service or hide failure as successful empty data.
- Azure Redis service/SKU availability must be verified before an externally
  managed resource is selected. No resource or lifecycle certification is made.
- CVE scanning was explicitly declined. This plan makes no claim that existing
  or migrated dependencies are vulnerability-free.
- Existing workshop infrastructure targets VMs, not these PaaS integrations;
  it is not treated as an available Azure test or deployment environment.
- Existing unrelated working-tree modifications must be preserved.

## Open Questions & Questionnaire

- [x] Q: Language and project scope? A: Inferred .NET/C#, website plus related
  database compatibility, from the report and current project.
- [x] Q: Static-content target? A: Azure Blob Storage.
- [x] Q: SQL target? A: Azure SQL Managed Instance, not Azure SQL Database.
- [x] Q: User authentication scope? A: Preserve existing user sign-in; exclude
  the unconfirmed Windows AD finding.
- [x] Q: Environment setup? A: No provisioning; code migration only.
- [x] Q: Integration testing? A: No; skip entirely. No baseline task is needed.
- [x] Q: Security/CVE remediation? A: Skip, explicitly overriding the default.
- [x] Q: Deployment target? A: No deployment; migration plan only.
- [x] Q: Containerization? A: No; skip.
- [x] Q: Output folder? A: .github/modernize/plans/report-20261023210809.
- [x] Q: Additional preferences? A: Preserve current behavior; no additional
  service scope. No new messaging, configuration or recommendation service.
- [x] Q: Confirm the nine-task summary and omitted task types? A: Confirm and
  save plan. No authorization to execute was given.

## Task File Validation

The supplied task schema uses closed objects inside `allOf`, which causes the
shared and task-specific fields to reject each other. Its upgrade definition
also has no `skills` property. Tasks therefore omit that property on the upgrade
task; upgrade execution is internal. Structural validation must check the
intended merged properties, required fields and dependency graph and disclose
that literal validation against the unmodified schema cannot succeed. No
upstream schema changes or additional project tooling are included in scope.
