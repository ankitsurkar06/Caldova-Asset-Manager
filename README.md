# Caldova-Asset-Manager

## Application identity for modernization assessments

Open [Caldova-Asset-Manager.sln](IaaS2PaaSWeb/Caldova-Asset-Manager.sln).
The web project is [Caldova-Asset-Manager.csproj](IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj);
its project name, assembly name, title, and product are `Caldova-Asset-Manager`.
Application code uses the `CaldovaAssetManager` C# namespace and the
`CaldovaAssetManagerContext` / `ICaldovaAssetManagerContext` persistence types.
The website lives in `IaaS2PaaSWeb/Caldova-Asset-Manager.Website`; retained database
deployment sources live in `IaaS2PaaSWeb/Caldova-Asset-Manager.Database`.
UI titles, navigation, logos and icons use the Caldova-Asset-Manager identity.
Existing SQL table mappings, connection-setting names, cookie schemes and the
data-protection application name remain compatible. This source rename does not
rename an existing database or Azure resource, migrate data, or deploy the app.

Run a fresh GitHub Copilot modernization assessment after the rename. Ignored reports,
previous publish packages, screenshots and Git history are historical outputs, not
the current application; regenerate reports/packages when needed. A report with no
projects may still show an unknown application until project discovery succeeds.
The web application now targets .NET 10 / ASP.NET Core and builds with the .NET
10 SDK. The old framework files remain as historical source but are excluded
from the modern web project. The workshop deployment project is separate and
still uses its original tooling.

## Build and run the modern web application

The current modernization scope is **webapp only**. It does not migrate to SQL
Managed Instance, provision dependent services, modify SQL scripts or create a
new database. A new Azure web host was subsequently approved and deployed as
described below. Local static assets and process-local caching remain in use.

```powershell
dotnet build .\IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj -c Release

# Supply a connection to your EXISTING SQL Server/database through the host.
# Do not put credentials in source-controlled files.
$env:ConnectionStrings__DefaultConnectionString = '<existing database connection>'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project .\IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj
```

ASP.NET Core configuration supports environment variables and host-injected
settings. `SQLCONNSTR_DefaultConnectionString` is also accepted for App Service.
There is no SQL MI-only validation or managed-identity requirement. A missing
connection setting fails explicitly at startup.
The application never creates, migrates or seeds the database at startup.

The application maps existing product/order tables and the ASP.NET Identity 2
user, role, claim and external-login schema. Existing password hashes are accepted
by ASP.NET Core Identity and upgraded after successful authentication. Users must
sign in again: old OWIN authentication/session cookies and outstanding account
verification tokens do not transfer to ASP.NET Core. No administrator is silently
created from demonstration credentials.

### Configuration and deployment prerequisites

- Run against a supported SQL Server with the existing application schema and
  validate connectivity, login, cart, checkout and administration before release.
  No live database was available during the webapp port.
- On Azure App Service, keys default to `HOME\data\Caldova-Asset-Manager\keys`.
  An explicit absolute `DataProtection__KeyPath` overrides this location. Outside
  App Service, non-Development startup requires that setting. The directory
  must be writable, durable and access-controlled; keys are sensitive.
  Multiple instances would need a shared key ring. Process-local session/cache behavior
  is preserved; use a single instance or suitable affinity until shared state is
  explicitly designed and validated.
- Existing optional social providers remain disabled unless
  `Authentication__<provider>__Enabled=true`, with externally supplied `ClientId`
  and `ClientSecret`. Provider names are `Facebook`, `Google`, `Microsoft` and
  `Twitter`. Configure their ASP.NET Core callback URLs with each provider before
  enabling them; external account identifiers remain mapped to existing records.
- Configure `Email__SmtpHost`, `Email__From`, optional `Email__SmtpPort` (587 by
  default), `Email__UserName` and `Email__Password` for confirmation, reset and
  email two-factor delivery. SMTP uses TLS. Missing delivery settings produce an
  explicit error, not a false claim that mail was sent.
- SMS delivery was a stub in the legacy application. It remains a deployment
  prerequisite for phone-based verification; the port reports its unavailability
  rather than silently accepting a phone number or pretending to send a code.
- Recommendations remain disabled by default. To enable them, provide
  `ShowRecommendations=true`, `MachineLearning__ScoringEndpoint`,
  `MachineLearning__ModelName` and `MachineLearning__AccountKey`. Confirm the
  legacy scoring endpoint still exists and supports the original contract before
  enabling the feature. Transport failures are logged and propagated; a valid
  response with no recommendations is still treated as an empty recommendation.
- Classic SignalR has been replaced with an ASP.NET Core hub at `/signalr`.
  Announcement broadcasts retain their `announcement` event and `Title`/`Url`
  payload. The optional announcement browser script remains disabled, as before.
  The old jQuery SignalR client is not compatible with the Core hub; replace it
  with the ASP.NET Core SignalR client before explicitly enabling that optional UI.
- Use HTTPS in production. Identity, session and antiforgery cookies are
  always Secure outside Development; Development HTTP remains supported.
  Native Windows App Service uses the web SDK's IIS integration for HTTPS
  termination. Do not enable unrestricted forwarded headers or clear trusted
  proxy lists. Set `AllowedHosts` for the deployed hostname. Do not use
  development error pages in production.

### Publish

```powershell
dotnet publish .\IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj `
    -c Release -o .\artifacts\webapp
```

Deploy the **published webapp output**, not the repository or the legacy
workshop VM project, to a .NET 10.0.13-or-newer ASP.NET Core host. IIS requires the
matching Hosting Bundle; other hosts can run `dotnet Caldova-Asset-Manager.dll`.
Static content is included in the publish output. The web SDK generates a new
IIS configuration; historical Web.config settings are not used by the Core app.
The Web Deploy profile requires explicit `CaldovaPublishServiceUrl` and
`CaldovaPublishSiteName` MSBuild properties, plus externally supplied deployment
authentication. It does not retain a previous site's endpoints or database
deployment settings. Existing deployments are not retargeted by a source rename.
The previous configuration is preserved in
[Web.Legacy.config](IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.Legacy.config), excluded
from publishing. The minimal [Web.config](IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config)
is only the input for the SDK's ASP.NET Core IIS hosting configuration.

`GET /health` reports process liveness only; it does not assert database or
external-service readiness. The app validates that a SQL setting is present at
startup but never opens the database just to serve this endpoint.

### App Service readiness and verification

The [modernization plan](.github/modernize/plans/app-service/plan.md) targets
native **Windows App Service, one instance**, keeping the existing SQL database.
The [deployment runbook](docs/AZURE-APP-SERVICE-DEPLOYMENT.md) describes required
settings, publish packaging, live release gates and rollback.

Run the focused hosting integration suite:

```powershell
dotnet test .\tests\Caldova.Hosting.Tests\Caldova.Hosting.Tests.csproj -c Release -p:NuGetAudit=false
```

The suite boots the real web application with isolated EF test data and a
pre-network SQL failure interceptor. It covers startup validation, production
cookies, authentication after a host restart with persisted keys, Development
HTTP, HTTPS redirects, host filtering, untrusted forwarding headers, assets,
production errors and SignalR negotiation. It does not certify real SQL or
Azure/IIS behavior. Branding checks also render login, catalog and cart pages,
verify the logo/favicon endpoints, and assert that the renamed context retains
the existing SQL mappings and data-protection application name.
`NuGetAudit=false` preserves the explicitly requested
exclusion of CVE scanning for this work; it is not a security certification.

On 2026-10-29, a new single-instance Windows B1 plan and web app were provisioned
in Korea Central, and the verified Release package was deployed. The actual
64-bit App Service runtimes satisfy the application's .NET 10 requirements.
**The app is intentionally stopped pending secure configuration of the existing
SQL connection**, as requested. The B1 plan still incurs charges while stopped.
See the [deployment evidence and configuration](.github/modernize/plans/app-service/infra/infra-config.md).

Live startup/HTTPS handling, SQL journeys and schema compatibility, storage
persistence across platform restarts, SMTP/SMS, social providers and remote
recommendations remain release gates. No database or optional service was
migrated, no existing hostname was cut over, and no CVE work was performed.

## Deployment of workshop environment.

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fkarlrissland%2FAppWorkshop%2Fmaster%2FIaaS2PaaSWeb%2FEnvironments%2FWorkshopEnv.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

This allows you to deploy the workshop environment.  When complete, you should have a Web Server and SQL server fully configured in their own vNet and Resource group.

## Prerequisites

There are no prerequistes for the deployment.

## Deployment steps

Click the "deploy to Azure" button at the beginning of this document.

Enter vm admin username and password

You can leave default values for the other parameters

NOTE: do not change the value of _artifactsLocation or _artifactsLocationSas as the deployment will fail.
