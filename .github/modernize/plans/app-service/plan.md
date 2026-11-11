# Modernization Plan: Caldova Asset Manager to Azure App Service

**Project**: Caldova-Asset-Manager  
**Updated**: 2026-10-27  
**Language**: dotnet  
**Status**: Azure package deployed; app intentionally stopped pending SQL settings

## Azure Deployment Phase Authorized on 2026-10-29

This authorization supersedes the earlier readiness-only restrictions **only**
for provisioning and deploying the web host. Earlier task results remain
historical evidence, not evidence of this cloud deployment.

- Provision a new native Windows App Service web app and a new B1 plan,
  one instance, in the existing resource group. Ongoing compute charges approved.
- Resource selection is recorded in [deployment configuration](infra/infra-config.md).
  [Bicep](infra/appservice.bicep) creates only the plan, web app and hosting settings.
- Preserve the current .NET 10 code, existing SQL database, schema and
  authentication. Database and other service modernization are deferred.
- Use Production, 64-bit hosting, HTTPS Only, TLS 1.2 minimum, durable App Service
  HOME keys, WebSockets and explicit host filtering. Disable basic publishing
  credentials and FTP; deploy with the operator's Azure identity.
- The operator supplies `DefaultConnectionString` securely through the portal.
  Do not persist credentials, use a fake connection, create a database, or
  treat an unconfigured process as a successful migration.
- New-app deployment does not redirect an existing production hostname or
  modify the previous host. If validation fails, do not cut over. Keep the
  previous host and database unchanged; stop the new app if necessary.
  A stopped B1 plan still incurs charges until the plan is deleted.

### Testing Strategy for the Deployment Phase

Reuse the frozen hosting baseline and existing xUnit/WebApplicationFactory
suite; no application behavior change or new test framework is planned.
Rerun all 16 cases and publish into a fresh output directory. Validate the
generated IIS configuration, runtime requirements and package root. On Azure,
verify actual runtime availability, production settings, HTTPS redirect,
`/health` JSON, static assets and SignalR negotiation. Validate persistence
across a controlled restart after initial startup succeeds.

Real SQL journeys (login, cart/checkout and administration), external providers,
SMTP and recommendations are deferred until their existing configuration and
network path are available. Do not write test records to the existing database.
Local mock tests cannot certify the SQL schema or Azure network path. Browser
journeys remain unverified until the existing database is connected. Liveness
alone is not acceptance of database-backed functionality.

### Deployment Clarifications

- [x] Q: Deployment scope? -> A: New App Service; database and others later.
- [x] Q: Provision paid compute? -> A: Windows B1, one instance, approved.
- [x] Q: Region and app name? -> A: Korea Central; caldova-asset-manager.
- [x] Q: SQL configuration? -> A: Operator configures the existing connection
  securely in the portal after provisioning.
- [x] Q: Tests? -> A: Existing local tests and live Azure checks; defer DB journeys.
- [x] Q: CVE remediation? -> A: Keep deferred; do not scan or change packages.
- [x] Q: Containers? -> A: No; retain native Windows .NET hosting.
- [x] Q: Restart/session limitations? -> A: Single-instance limitations accepted.
- [ ] Confirm startup and live hosting after secure SQL configuration.
- [ ] Confirm SQL connectivity and critical journeys before production cutover.

### Deployment Outcome on 2026-10-29

The approved B1 plan and web app were provisioned successfully. All 16 existing
hosting tests passed again (zero failures/skips), and a fresh Release publish
passed generated IIS, runtime, static-content and ZIP-root checks. Authenticated
SCM inspection confirmed 64-bit Microsoft.NETCore.App and Microsoft.AspNetCore.App
10.0.13 and 10.0.14 on the actual host.

The operator then confirmed SQL configuration was not ready and explicitly
requested: deploy the package but leave the app stopped. ZIP deployment completed
successfully; an independent read of the persisted deployment confirmed status 4,
complete and active. The app was stopped and its final **Stopped** state verified.
This is a successful artifact deployment, **not** a startup or production-readiness
certification. No database or production hostname was changed.

Live HTTPS/application probes, restart/key persistence, real SQL journeys and
browser validation remain unexecuted. Log Analytics retrieval was unavailable
because the resource group has no Log Analytics workspace; none was provisioned.
See [deployment configuration and evidence](infra/infra-config.md) for the
deployment identity and artifact checksum.

## Technical Framework

- **Language/runtime**: C# / .NET 10 (`net10.0`).
- **Framework**: ASP.NET Core MVC, Identity, and SignalR.
- **Build tool**: .NET SDK and SDK-style web project.
- **Database**: Existing SQL Server-compatible database through EF Core 10.
- **Key dependencies**: ASP.NET Core Identity/EF Core SQL Server 10.0.0,
  Newtonsoft.Json 13.0.3, and System.Security.Cryptography.Pkcs 10.0.13.
- **Deployment unit**: The published website, not the workshop solution,
  database project, or VM deployment project.

Current source of truth:

- [Website project](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj)
- [Application startup](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Program.cs)
- [Application settings](../../../../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/appsettings.json)
- [Current build and hosting documentation](../../../../README.md)

The [historical assessment](../../../../docs/AZURE-APP-SERVICE-ASSESSMENT.md)
describes the former .NET Framework 4.5.1/System.Web application. Its framework,
LocalDB, startup seeding, legacy build-tool, and classic SignalR findings do not
describe the active .NET 10 project and are not carried forward as current
defects. App Service configuration, SQL reachability, state persistence, and
release validation remain relevant and were checked against current startup.
No successful Azure deployment or live-dependency validation is assumed.

## Overview

Prepare the current web application for an existing **Windows Azure App Service
web app hosted by an existing App Service plan**, using native .NET 10 hosting.
An App Service plan supplies compute; the web app is the deployment target.
Preserve application features and the existing database without introducing
new Azure service migrations.

The migration will:

- Prepare the website for externally supplied production configuration.
- Preserve authentication, database compatibility, and published static assets.
- Establish migration-focused baseline and verification evidence.
- Publish and locally validate a repeatable Release artifact.
- Document a future rollback path without changing the database schema.

The user authorized code changes and local/mock verification, not resource
provisioning or Azure deployment. Deployment, cloud rollback validation and live
dependency checks are future release gates, not tasks executed in this plan.

Detailed, dependency-ordered execution work is in
[tasks.json](.metadata/tasks.json), not in this document.

## Migration Impact Summary

| Application | From | Azure target | Authentication | Scope |
|-------------|------|--------------|----------------|-------|
| Caldova web | Current host | Windows App Service | Existing | Web only |
| SQL access | Existing SQL | Same database | Existing | Connectivity |

End-user authentication and database authentication remain unchanged unless a
separate change is approved. Managed identity adoption is not implicitly added.

## Target Hosting and Release Constraints

- Before future deployment, use an existing Windows App Service plan and
  compatible web app. Record their environment details in
  `infra/infra-config.md` under this plan before deployment; do not put
  credentials in the plan or task file.
- Verify the selected region, plan SKU, capacity, Windows OS, deployment
  permissions, and native .NET 10 hosting support before deployment.
  The project pins runtime 10.0.13. Confirm runtime roll-forward behavior and
  that the actual host satisfies the published application's runtime needs.
  Do not silently switch to Linux, containers, another framework, or create
  resources if the existing target is unsuitable.
- Publish only the active web project in Release mode. Use the web SDK's
  generated IIS configuration, not historical System.Web transforms.
- Start with one instance and no autoscale expansion. Session and cache use
  process-local storage; restarts can lose anonymous cart/session continuity.
  Affinity does not make this state durable. Obtain explicit acceptance of
  restart/session behavior before production. A requirement for durable
  sessions or multi-instance operation needs separate approved scope.
- Preserve `/signalr` behavior. Enable and verify WebSockets if used. Do not
  enable the optional incompatible legacy browser client or claim that
  single-instance verification establishes scale-out readiness.
- Persist data-protection keys to durable, access-controlled storage available
  on the existing host. Verify authentication after restart. Keep key rings
  separate between validation and production unless deliberate, reviewed
  cookie-sharing behavior is required.

## Configuration and Existing Dependencies

- Supply `ConnectionStrings__DefaultConnectionString`, or the supported
  App Service SQL connection-string setting `DefaultConnectionString`.
  Confirm the configuration provider and effective key resolve on the target.
  Missing, empty or whitespace-only configuration must fail at startup rather
  than letting liveness imply that a usable application configuration exists.
- Keep the existing SQL database and schema. Validate encrypted connectivity,
  authentication, least-privilege access, firewall rules, DNS, and routing
  from App Service. Private/on-premises SQL access must already have a usable
  network path; missing connectivity blocks release rather than triggering
  unapproved network provisioning or database migration.
- Use `ASPNETCORE_ENVIRONMENT=Production` and restrict `AllowedHosts` to approved
  hostnames. An explicit absolute `DataProtection__KeyPath` takes precedence;
  otherwise, App Service must use a dedicated directory beneath persistent HOME.
  Production outside App Service requires an explicit durable path.
- Validate HTTPS termination and trusted proxy/forwarded-header handling on
  App Service. Redirects, secure cookies, and external-provider callback
  URLs must reflect the public HTTPS address without trusting arbitrary
  forwarded headers.
  Native Windows hosting uses the web SDK's IIS integration. Do not clear the
  trusted-proxy lists or enable unrestricted forwarded-header processing.
  Identity and session cookies must be Secure and HttpOnly in production;
  Development HTTP remains supported.
- Keep recommendations and optional social authentication disabled unless
  explicitly required and fully configured. Preserve enabled integrations
  using external secrets, approved endpoints, and valid callback addresses.
- Validate SMTP configuration if account confirmation, reset, or email
  two-factor flows are required. SMS remains unavailable in current code;
  phone-verification-dependent release requirements are a blocker, not an
  implicit implementation task.
- Use existing approved secret injection mechanisms. A new Key Vault, managed
  identity migration, telemetry service, or CI/CD platform is not introduced.
- Collect platform/application logs and define release alerts using available
  facilities. Avoid logging connection strings, tokens, or customer data.

## Validation and Acceptance

The baseline task snapshots only the main website directory before code changes.
Its frozen specification records existing behavior and a real/mock decision
table for SQL, SMTP, optional social providers, recommendations, and hosting
facilities. Baseline capture and hosting preparation are independent; baseline
capture must take its snapshot before source mutation begins.

Mock-based integration verification covers changed hosting/configuration
behavior and preserves the baseline's scope. Mock results are not evidence of
Azure connectivity, real SQL schema compatibility, or successful deployment.
No existing test suite was identified during planning; do not report tests as
passing until a test run exists.

Release gates:

1. Release build and publish succeed, including Razor views and static assets.
2. Missing required configuration fails explicitly; production error handling,
   public HTTPS redirects, authorization, and secret handling are verified.
3. The approved artifact starts on the existing target and `/health` responds.
   This endpoint is process liveness only, not database readiness.
4. Mandatory live checks establish SQL connectivity and exercise login,
   product browsing, cart, checkout, order persistence, and authorized admin
   access against an approved test dataset. Avoid customer-affecting test orders.
5. Validate SignalR negotiation/transport and configured optional integrations.
   Record intentionally disabled integrations rather than claiming they passed.
6. Restart verification demonstrates persistent data-protection keys and records
   the accepted process-local session/cart limitations.
7. Deployment, rollback evidence, and any remaining release risks receive owner
   approval before production cutover. CVE remediation was explicitly excluded;
   this plan does not constitute a security certification.

## Deployment, Cutover, and Rollback

Identify the existing target and its owners before execution. Use an existing
nonproduction web app or existing deployment slot when available and supported.
Do not create a slot or assume one is available. Without a safe validation target,
obtain approval for a maintenance-window strategy before deploying to production.
Mark connection strings, secrets, and environment-specific settings appropriately
if slot swaps are used, and validate warm-up behavior before any swap.

Retain the previous known-good artifact and a secure record of configuration
needed to restore it. No schema/data migration or startup database seeding is
part of this plan. Define rollback triggers for startup failure, redirect loops,
SQL/authentication failures, or failed critical journeys. Roll back by restoring
the prior artifact/configuration or reversing a validated slot swap. Recheck
liveness and critical journeys, and preserve incident logs. If returning to the
older framework app, explicitly account for incompatible cookies/tokens and
require sign-in again; do not promise cross-version session continuity.

## Scope Boundaries and Execution Gates

Included: web-hosting preparation, migration-focused baseline and integration
verification, a local publish artifact, and instructions for future deployment.

Excluded: runtime upgrade, SQL Managed Instance or other database migration,
schema changes, infrastructure generation/provisioning, containers, shared
session/backplane implementation, CVE scanning/remediation, and unrelated fixes.
One website is in scope; no topology grouping or application decomposition is
needed for this hosting-only migration. Live deployment is also excluded.

Readiness implementation may proceed locally. Future production release remains
blocked until the target runtime, SQL connectivity, durable storage, actual IIS
HTTPS handling, and safe live-validation environment have been verified.
Task results and remaining gates are recorded in the verification summary.

## Execution Outcome

The approved code-readiness scope completed on 2026-10-27: 16 hosting integration
cases executed and passed, Release publishing succeeded, and local processes
started from the published artifact passed their scoped HTTP checks. The ZIP is
`artifacts/Caldova-Asset-Manager-AppService.zip`; it contains the verified
framework-dependent application at its root.

The implementation adds startup SQL-setting validation, durable App Service
key-ring defaults with explicit override/failure rules, and production Secure
identity/session/antiforgery cookies. Packaging excludes historical project
metadata. No runtime, SQL schema, shared-state service or optional integration
was migrated. No live Azure operation or CVE scan was performed.

See [verification evidence](003-integrationTest/verification-summary.md) and
the [deployment runbook](../../../../docs/AZURE-APP-SERVICE-DEPLOYMENT.md).

## Open Questions & Questionnaire

- [x] Q: Where should the plan be saved? -> A:
  `.github\modernize\plans\app-service`.
- [x] Q: Should infrastructure be provisioned? -> A: Use existing App Service
  resources; identify them before execution. Do not provision resources.
- [x] Q: Which deployment target and OS? -> A: Windows Azure App Service,
  native .NET 10, without containers.
- [x] Q: Should integration testing be included? -> A: Mock dependencies
  initially; live verification is required before production.
- [x] Q: Should CVE remediation be included? -> A: Skip CVE remediation.
- [x] Q: Should the runtime or database be migrated? -> A: Retain the current
  .NET 10 application and existing SQL database, as confirmed in scope review.
- [x] Q: What should execute now? -> A: Confirmed on 2026-10-27: code, tests,
  local publish artifact and deployment instructions only. No cloud writes.
- [x] Q: Is the revised task summary correct? -> A: Confirmed: baseline,
  hosting transform, and local/mock integration verification. No runtime
  upgrade, infrastructure, deployment, containerization or CVE task.
- [x] Q: Which test dependency/authentication decisions apply? -> A: All
  external dependencies mock, authentication n/a in tests; no live credentials.
- [x] Q: Are startup validation, durable key storage and secure-cookie behavior
  changes approved? -> A: Yes; preserve process-local session/cache limitations.
- [ ] Identify the existing App Service plan/web app and deployment environment.
- [ ] Confirm host runtime support, capacity, network access, and key storage.
- [ ] Identify the safe live-validation target, test data, and release owner.
- [ ] Approve restart-related session limitations and rollback/cutover strategy.
