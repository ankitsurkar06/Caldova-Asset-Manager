# Windows Azure App Service deployment runbook

## New-host deployment authorized on 2026-10-29

The user subsequently approved a **new Windows B1 plan and web app**, one
instance, in the existing resource group. This extends the earlier existing-host
scope below; it does not authorize database or other service modernization.
See the [approved configuration and provisioning command](../.github/modernize/plans/app-service/infra/infra-config.md)
and [Bicep template](../.github/modernize/plans/app-service/infra/appservice.bicep).
The template contains no SQL secret and leaves connection strings unchanged.

After provisioning, open the new app in the Azure portal, select **Settings >
Environment variables > Connection strings**, and add `DefaultConnectionString`
with the **existing** SQL connection using SQLServer or SQLAzure as appropriate.
Apply the change. Do not paste credentials into chat or source files.
The existing database must be reachable from the new host; this deployment does
not add network connectivity. Without the real connection, the application
intentionally fails startup, and live validation remains blocked.

Security/CVE work remains deferred by explicit user choice. Existing in-memory
session/restart limitations are accepted. Do not move production hostnames until
live SQL journeys and the remaining production release gates are satisfied.

**Deployment outcome:** The new Windows B1 plan and web app were provisioned
in Korea Central on 2026-10-29. All 16 local hosting tests passed and the fresh
Release ZIP was successfully deployed. Actual 64-bit .NET/ASP.NET Core 10.0.13
and 10.0.14 were confirmed on the host. The operator reported SQL configuration
was not ready and requested deployment with the app left stopped. The final
**Stopped** state and successful persisted deployment metadata were verified.
See [deployment evidence](../.github/modernize/plans/app-service/infra/infra-config.md).
Startup, live application checks and real SQL readiness remain unverified.
Configure the real connection securely before starting the app and executing
the release gates below. **The B1 plan is billed even while the app is stopped.**

## Scope and readiness boundary

This runbook deploys only the .NET 10 website to an **existing native Windows
App Service web app**, on **one instance**, using the **existing SQL database**.
It is not the workshop VM deployment and does not create resources, containers,
database schema, sample data or administrator accounts.

See the [readiness plan](../.github/modernize/plans/app-service/plan.md),
[task results](../.github/modernize/plans/app-service/.metadata/tasks.json) and
[local verification evidence](../.github/modernize/plans/app-service/003-integrationTest/verification-summary.md).
Local tests and publishing do not certify live Azure or database readiness.
CVE scanning/remediation was explicitly excluded.

## Existing-host prerequisites

- Confirm target ownership, deployment permission, Windows OS and native
  .NET 10 support in the selected App Service environment. The framework-dependent
  artifact requires **Microsoft.NETCore.App and Microsoft.AspNetCore.App
  10.0.13 or a compatible later 10.0 patch**. Check installed versions on the
  actual host; do not assume the portal stack label guarantees this minimum.
  An unsupported host blocks deployment; do not silently change runtime or OS.
- Use a compatible 64-bit worker and one running instance. Disable autoscale
  expansion. ARR affinity does not preserve process-local state on restart.
  In-memory session/cache and SignalR have not been made scale-out ready.
- Enable HTTPS Only and an approved minimum TLS version. Native IIS hosting
  obtains the public scheme through ASP.NET Core's IIS integration. Do not set
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, clear known proxy lists or trust
  arbitrary client `X-Forwarded-*` headers. A separate front proxy requires its
  own reviewed trust configuration; that topology was not implemented here.
- Enable Always On if the existing plan supports it. Enable WebSockets if the
  Core SignalR hub is used. The optional old browser SignalR client remains
  disabled and is not compatible with this hub.
- Verify the existing database network route, DNS, firewall/private endpoint
  access, TLS trust and application identity permissions from App Service.
  This task does not establish a VNet integration or move the database.
- Confirm durable HOME storage is enabled and writable. The app must not run
  with an ephemeral replacement for that path.

## Required external configuration

Configure these on the **existing** app using approved platform settings/secret
injection. Do not put credentials in checked-in files, deployment ZIPs, logs or
command history. App Service Key Vault references may supply values if already
available; the application does not add a Key Vault provider or create a vault.

| Setting | Required value or behavior |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production`. Remove a conflicting `DOTNET_ENVIRONMENT`, or set it to `Production` as well; minimal hosting gives it precedence. |
| `AllowedHosts` | Exact approved public hostname(s), separated by semicolons. Include the default app hostname if it is used for validation/health probes. Do not leave `*` in the deployed configuration. |
| Connection string named `DefaultConnectionString` | Existing SQL connection, injected as an App Service SQL/SQLAzure connection-string setting. Alternatively use application setting `ConnectionStrings__DefaultConnectionString`. Choose one authoritative source, not conflicting aliases. |
| `DataProtection__KeyPath` | Normally leave unset on App Service: the app detects `WEBSITE_INSTANCE_ID` and uses `Path.Combine(HOME, "data", "Caldova-Asset-Manager", "keys")`. For an override, supply an actual absolute durable path. Environment expressions such as `%HOME%` are not expanded in an override. |
| `HOME`, `WEBSITE_INSTANCE_ID` | Platform-provided values; do not replace them with local test values. Missing HOME without an explicit key path fails startup on App Service. |

SQL configuration must be nonempty at startup. This check is not a connection,
TLS, permission or schema test. Use encrypted SQL connections with certificate
validation (`Encrypt=True`, `TrustServerCertificate=False`) according to the
existing database's supported authentication configuration. No SQL MI-only or
managed-identity requirement is introduced.

Outside App Service, a non-Development host requires an explicit absolute key
path. Missing/relative key paths and inaccessible directories fail rather than
quietly using an ephemeral ring. **Back up and restrict the key directory**:
XML key material is sensitive, and explicit filesystem persistence does not
add application-level key encryption. Use the approved storage protection and
access controls. Keep validation/production and unrelated apps isolated; do not
delete keys during deployment. Test slot/restart behavior on the actual topology.

Identity, session and antiforgery cookies are Secure outside Development.
The existing application name is unchanged to retain compatible Core cookies
when the same key ring is available. Old OWIN cookies/tokens from the historical
framework app remain incompatible. A stable key ring preserves authentication,
**not** in-memory session/cart continuity.

## Optional integrations

Leave integrations disabled unless their existing service is required and tested.

- Recommendations: `ShowRecommendations`, `MachineLearning__ScoringEndpoint`,
  `MachineLearning__ModelName`, `MachineLearning__AccountKey`.
- Social providers: `Authentication__<provider>__Enabled`, `ClientId` and
  `ClientSecret`, for Facebook, Google, Microsoft and Twitter. Register the
  correct public HTTPS ASP.NET Core callback URLs with each provider.
- Email: `Email__SmtpHost`, `Email__From`, optional `Email__SmtpPort` (587),
  `Email__UserName`, `Email__Password`. Validate TLS and delivery for password
  reset/confirmation/2FA if those flows are required.
- SMS remains explicitly unavailable; do not enable a release requirement
  that depends on successful phone verification without separate implementation.

## Build, test and package

Use the .NET 10 SDK. Run from the repository root in PowerShell:

```powershell
dotnet test .\tests\Caldova.Hosting.Tests\Caldova.Hosting.Tests.csproj `
    -c Release -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Hosting verification failed.' }

# Use a fresh output directory so files from an older deployment cannot linger.
$publish = Join-Path (Get-Location) ('artifacts\webapp-' + [guid]::NewGuid().ToString('N'))
dotnet publish .\IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj `
    -c Release --no-restore -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }

$zip = Join-Path (Get-Location) ('artifacts\Caldova-Asset-Manager-' + [guid]::NewGuid().ToString('N') + '.zip')
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip
```

`NuGetAudit=false` is a deliberate execution option for this user-scoped task,
not a repository-wide policy. A separate security release review is still
recommended before production.

Package the **contents** of the publish directory, not its parent. The root
must contain the application DLL, runtime/dependency JSON and the **generated**
IIS `web.config`. It uses `AspNetCoreModuleV2`, `dotnet`,
`.\Caldova-Asset-Manager.dll` and in-process hosting. Razor views are compiled.
Content, Images, Scripts, fonts and the favicon must be present.
Historical XML configuration, `packages.config`, and `Models/project.json`
are excluded. Never deploy the repository, old bin directory, solution output,
VM templates or an old Web Deploy profile as this artifact.

An optional manual upload command, **not executed by this task**, is:

```powershell
az webapp deploy --resource-group <existing-resource-group> `
    --name <existing-webapp> --src-path $zip --type zip
```

Use an existing approved validation app/slot first. This runbook does not
authorize creating a slot or changing a live production app without release
approval. If no safe target exists, obtain a maintenance window.

## Mandatory live release gates

Record evidence on the approved target before cutover:

1. Confirm required host runtimes, effective non-secret settings, one-instance
   capacity and generated IIS configuration.
2. Start the app; `GET /health` must return 200 and `{"status":"running"}` over
   public HTTPS. The endpoint is **process liveness only**, not SQL readiness.
   App Service Health Check can use `/health` with this limitation explicitly
   accepted; monitor dependency failures separately.
3. Check public HTTP redirects to the correct HTTPS hostname without loops.
   Verify Secure/HttpOnly cookies, HSTS, hostname filtering, login return URLs
   and any enabled provider's HTTPS callbacks through actual IIS/platform routing.
4. Prove SQL connectivity/TLS/schema compatibility and run login, browsing, cart,
   checkout, persisted-order lookup and authorized administration against
   approved test data. Do not place customer-affecting test orders.
5. Verify `/signalr` negotiation and the required transport. For an invalid
   negotiation version, SignalR returns HTTP 200 with a protocol error JSON;
   HTTP 200 alone does not establish a usable connection.
6. Restart/recycle the actual app. Confirm that keys remain in the approved
   persistent directory and a previously issued Core identity cookie still
   authenticates. Separately record the **accepted loss of in-memory sessions**:
   anonymous cart rows may remain in SQL while their former session ID is lost.
7. Test only enabled remote integrations. Disabled integrations are not passes.
   Verify error logs without sensitive payloads and available platform alerts.
8. Obtain owner approval for remaining risks, restart behavior and rollback.

## Rollback

Retain the previous known-good artifact and a secure record of its external
settings and key ring. No database changes are part of this release.
Rollback triggers include startup failure, redirect loops, lost authentication,
failed SQL journeys or unacceptable error rates. Restore the prior
artifact/configuration or reverse a previously validated existing-slot swap.
Recheck health and critical journeys and preserve incident logs. Do not remove
or regenerate the key ring as an attempted repair. Rollback to the historical
System.Web application requires users to sign in again.

## Historical assessment

[The earlier assessment](AZURE-APP-SERVICE-ASSESSMENT.md) describes the former
.NET Framework application. Its System.Web, LocalDB, framework build-target and
startup-seeding findings are not current defects in this .NET 10 web project.
The new source-based facts and the plan use the current working tree instead.
