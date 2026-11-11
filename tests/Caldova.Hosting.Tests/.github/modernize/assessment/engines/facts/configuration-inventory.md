# Configuration & Externalized Settings Inventory

Caldova Asset Manager reads defaults from one `appsettings.json` file and relies on host-provided configuration for database and optional integration credentials. No environment-specific `appsettings` files, container manifests, or deployment resource limits were found.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| Default application settings | JSON | `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/appsettings.json` | Contains empty connection-string section, `ShowRecommendations=false`, `SiteDelayMilliseconds=0`, logging levels and `AllowedHosts=*` |
| Host configuration | Environment variables / ASP.NET Core configuration | Runtime environment | Supports standard double-underscore key mapping; database also supports `SQLCONNSTR_DefaultConnectionString` |
| Legacy web configuration | XML / transforms | `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Web.Legacy.config` | Historical files; the SDK project removes `*.config` from active content and excludes legacy source components |
| Hosting test configuration | Test fixture | `tests/Caldova.Hosting.Tests/test-cases/testdata/configuration/hosting.json` | Test-only values, including disabled optional OAuth providers |
| Key Vault reference documentation | Documentation | `docs/KEY-VAULT-SECRETS.md` | Describes an external-secret helper that is excluded from the current SDK build; no active Key Vault provider registration was found |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | MSBuild `Debug` configuration | Development/debug build | No conditional package set identified |
| Release | MSBuild `Release` configuration | Optimized/publish build | No conditional package set identified |

No custom MSBuild profile, conditional compilation symbol, or package version central-management file was found.

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Development | `ASPNETCORE_ENVIRONMENT=Development` | No `appsettings.Development.json` present | Cookies use `SameAsRequest`; HSTS and HTTPS redirect middleware are not enabled |
| Production/non-development | Host environment; default when unset | No environment-specific appsettings file present | Requires durable Data Protection key storage; HSTS and HTTPS redirection are enabled |

## Properties Inventory

### Website

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `ConnectionStrings:DefaultConnectionString` | Not configured | Runtime | Host configuration; `SQLCONNSTR_DefaultConnectionString` is a fallback |
| `DataProtection:KeyPath` | Not configured | Runtime | Host configuration; App Service may use `HOME`-based durable storage |
| `ShowRecommendations` | `false` | All | `appsettings.json` or host configuration |
| `SiteDelayMilliseconds` | `0` | All | `appsettings.json`; nonnegative integer |
| `AllowedHosts` | `*` | All | `appsettings.json` |
| `Authentication:{Facebook,Google,Microsoft,Twitter}:Enabled` | False when not supplied | Runtime | Host configuration; provider is added only when enabled |
| `Authentication:{provider}:ClientId` / `ClientSecret` | Not configured | Runtime | Host configuration; required for an enabled provider |
| `MachineLearning:ScoringEndpoint` / `ModelName` / `AccountKey` | Not configured in active `appsettings.json` | Optional recommendations | Host configuration; endpoint must be HTTPS; recommendation feature is disabled by default |
| `Email:SmtpHost` / `From` / `SmtpPort` / `UserName` / `Password` | Host not configured; port defaults to 587 | Account email delivery | Host configuration; SMTP credentials are optional when anonymous relay is used |
| `Logging:LogLevel:Default` | `Information` | All | `appsettings.json` |
| `Logging:LogLevel:Microsoft.AspNetCore` and `Microsoft.EntityFrameworkCore` | `Warning` | All | `appsettings.json` |

The legacy `ExternalSecretConfiguration` supports `CALDOVA_*` name conversion, but that source is excluded from the active project compilation and should not be treated as the current secret-resolution mechanism.

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| Website | .NET 10; host controls environment and process options | Not specified | Not specified |

No JVM parameters, container limits, CPU requests, or instance-count settings were found.

## Startup Dependency Chain

1. The host supplies configuration before application startup.
2. The web host requires a valid SQL Server connection string to register the EF Core context; actual database availability is required for database-backed operations.
3. Non-development environments require a durable absolute Data Protection key path (or App Service `HOME`) for persistent keys.
4. SMTP, external OAuth, and the Azure ML endpoint are only needed when the corresponding feature is enabled or invoked; no readiness wait or retry chain is configured.

No Docker Compose `depends_on`, Kubernetes readiness probe, or startup wait utility was found.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `ConnectionStrings:DefaultConnectionString` / `SQLCONNSTR_DefaultConnectionString` | Database connection | Host-supplied; no value checked in |
| `Authentication:{provider}:ClientSecret` | OAuth client credential | Host-supplied; no value checked in |
| `MachineLearning:AccountKey` | Recommendation API credential | Host-supplied; recommendation feature defaults off |
| `Email:Password` | SMTP credential | Host-supplied; no value checked in |

### Secrets Provisioning Workflow

The active application reads settings through ASP.NET Core configuration, so the deployment host must inject connection strings and optional credentials. The repository contains Key Vault secret-resolution code and documentation, but the code is explicitly excluded from the current SDK project compilation; no active managed-identity/Key Vault provisioning flow is wired into this application build. No actual secret values are included in this inventory.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| `ShowRecommendations` | `false` | `appsettings.json` or host configuration |
| `Authentication:Facebook:Enabled` | false unless enabled | Host configuration |
| `Authentication:Google:Enabled` | false unless enabled | Host configuration |
| `Authentication:Microsoft:Enabled` | false unless enabled | Host configuration |
| `Authentication:Twitter:Enabled` | false unless enabled | Host configuration |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET target framework | `net10.0` | `Caldova-Asset-Manager.csproj` |
| .NET runtime framework | `10.0.13` | `Caldova-Asset-Manager.csproj` |
| ASP.NET Core Identity EF | `10.0.0` | `Caldova-Asset-Manager.csproj` |
| EF Core SQL Server | `10.0.0` | `Caldova-Asset-Manager.csproj` |
| Newtonsoft.Json | `13.0.3` | `Caldova-Asset-Manager.csproj` |
| System.Runtime.Caching | `10.0.0` | `Caldova-Asset-Manager.csproj` |
| Hosting tests and EF InMemory | `10.0.0` | `tests/Caldova.Hosting.Tests/Caldova.Hosting.Tests.csproj` |
