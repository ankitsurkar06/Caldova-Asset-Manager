# Approved App Service deployment configuration

- Subscription: `4bd2aa0f-2bd2-4d67-91a8-5a4533d58600`
- Existing resource group: `CaldovaRG`
- Region: `koreacentral`
- New Windows web app: `caldova-asset-manager`
- New Windows plan: `asp-caldova-asset-manager`
- SKU: B1; capacity: one; ongoing charges approved.
- Runtime: native .NET 10, 64-bit; verify actual installed patch before deployment.
- Database: existing SQL Server/database, unchanged.
- Connection delivery: operator adds App Service connection string
  `DefaultConnectionString` securely in the portal; no secret is recorded here.
- No custom-domain cutover, database provisioning, network provisioning,
  optional service migration, containers or security/CVE work authorized.

## Provisioning

Run from the repository root with the approved subscription accessible:

```powershell
az deployment group create `
    --subscription 4bd2aa0f-2bd2-4d67-91a8-5a4533d58600 `
    --resource-group CaldovaRG `
    --name caldova-appservice `
    --template-file .\.github\modernize\plans\app-service\infra\appservice.bicep `
    --parameters appName=caldova-asset-manager location=koreacentral
```

The template supplies initial app settings. Reapplying it replaces the app
settings collection: preserve later optional settings before any reapplication.
It does not declare or replace the separate connection-string collection.
It creates no database and includes no secrets.

The app must not be declared operational until the real SQL setting is supplied
and deployment/startup verification succeeds. The existing database network path
and real user journeys are independent production release gates.

## Verified deployment outcome, 2026-10-29

- ARM deployment: `caldova-appservice`, Succeeded.
- App resource ID:
  `/subscriptions/4bd2aa0f-2bd2-4d67-91a8-5a4533d58600/resourceGroups/CaldovaRG/providers/Microsoft.Web/sites/caldova-asset-manager`
- Hostname: `caldova-asset-manager.azurewebsites.net`.
- Plan confirmed: B1, one instance, Korea Central.
- Actual 64-bit host runtimes confirmed through Azure-authenticated SCM:
  Microsoft.NETCore.App and Microsoft.AspNetCore.App 10.0.13 and 10.0.14.
- Hosting confirmed: .NET v10.0, 64-bit, Always On, WebSockets, HTTPS Only,
  affinity, TLS/SCM TLS 1.2 minimum, FTP disabled, health path `/health`.
- Local tests: `dotnet test .\tests\Caldova.Hosting.Tests\Caldova.Hosting.Tests.csproj -c Release -p:NuGetAudit=false`;
  exit 0, 16 passed, zero failed/skipped.
- Fresh artifact:
  `artifacts\Caldova-Asset-Manager-Azure-20261029.zip`, 220 entries.
- SHA-256:
  `333B191E1717F85A64B161943C5FA3D79272DAB5900C55EA319F7DE50D953E50`.
- Deployment ID: `048c9c5bbfbc440291d202f460f1554e`.
- Deployment completed at `2026-10-29T13:01:29.9446207Z`; independently read
  SCM metadata confirmed status 4 (success), complete and active.
- Final web app state: **Stopped**, verified after deployment as explicitly
  requested because the SQL connection was not yet configured.
- No placeholder SQL connection, database changes or production cutover.
- Startup, live HTTPS/SignalR/browser checks, restart/key persistence and real
  SQL journeys: **UNVERIFIED**. Do not start until secure configuration is ready.
- Log Analytics: unavailable; no workspace exists in the resource group.
  No new monitoring service was provisioned.

Once the real connection is applied, start this web app through the Azure portal
and complete the runbook's live hosting gates. Liveness does not prove SQL
connectivity. The B1 plan continues to incur compute charges while the app is stopped.
