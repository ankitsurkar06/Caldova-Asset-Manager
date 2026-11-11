# Configuration & Externalized Settings Inventory

Runtime configuration is assembled from ASP.NET Core defaults, `appsettings.json`, and host-provided settings. Authentication credentials, database connection details, and optional recommendation settings are expected to be supplied externally rather than embedded in the active settings file.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| ASP.NET Core settings | JSON | `Caldova-Asset-Manager.Website/appsettings.json` | Logging, allowed hosts, recommendation default, and empty connection-string section |
| Environment overrides | Environment variables | Hosting environment | Supports standard .NET configuration overrides and `SQLCONNSTR_DefaultConnectionString` |
| Website project | Build configuration | `Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj` | Targets .NET 10 and declares active package references |
| SQL project | Build/deployment configuration | `Caldova-Asset-Manager.Database/Caldova-Asset-Manager.Database.sqlproj` | Contains ReadyRoll project settings and SQL deployment artifacts |
| Legacy web settings | XML / transforms | `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Web.Legacy.config` | Historical configuration artifacts; not the active ASP.NET Core configuration source |
| Legacy package manifest | NuGet XML | `Caldova-Asset-Manager.Website/packages.config` | Old Framework-era package list removed from the current SDK project items |

No environment-specific `appsettings.{Environment}.json`, checked-in Kubernetes configuration, Compose file, or active external configuration repository was found in the target workspace.

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | MSBuild `Configuration=Debug` | Development compilation and symbols | .NET SDK project |
| Release | MSBuild `Configuration=Release` | Optimized publish output | .NET SDK project |
| Database Debug / Release | SQL project configuration | Build and deploy schema artifacts | ReadyRoll / SQL Server Data Tools targets |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Development | `ASPNETCORE_ENVIRONMENT=Development` or host equivalent | `appsettings.json` plus standard optional environment file resolution | Development-specific HTTPS and key-storage behavior |
| Non-Development | Any non-Development hosting environment | `appsettings.json` plus standard optional environment file resolution | Exception handler, HSTS, HTTPS redirection, and durable data-protection key requirement |

No checked-in environment-specific settings files were found. `WebApplication.CreateBuilder` uses the normal ASP.NET Core environment-selection behavior.

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `ConnectionStrings:DefaultConnectionString` | Empty/not set | All | External settings; alternatively `SQLCONNSTR_DefaultConnectionString` |
| `DataProtection:KeyPath` | Unset | Non-Development requires a durable location except App Service fallback | Host configuration |
| `Authentication:{Provider}:Enabled` | False when absent | All | External configuration for Facebook, Google, Microsoft, or Twitter |
| `Authentication:{Provider}:ClientId` | Unset | Required when provider enabled | External configuration |
| `Authentication:{Provider}:ClientSecret` | Unset | Required when provider enabled | External secret configuration |
| `ShowRecommendations` | `false` | All | `appsettings.json` or host override |
| `MachineLearning:AccountKey` | Unset | Required when recommendations enabled | External secret configuration |
| `MachineLearning:ModelName` | Unset | When recommendations requested | External configuration |
| `MachineLearning:ScoringEndpoint` | Unset | When recommendations requested | External configuration; endpoint must be HTTPS |
| `AllowedHosts` | `*` | All | `appsettings.json` |
| `Logging:LogLevel:*` | Information / framework warning defaults | All | `appsettings.json` and host overrides |
| `HOME`, `WEBSITE_INSTANCE_ID` | Host-provided | App Service | Used to select durable key storage when no explicit key path is set |

Secret values are intentionally not reproduced in this inventory.

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| Website | .NET runtime 10.0.13; no custom command-line options identified | Not specified in repository | Not specified |
| Database | SQL Server runtime managed outside the workspace | Not specified in repository | Not specified |

## Startup Dependency Chain

1. The host supplies the database connection string and any enabled external-provider or recommendation settings.
2. ASP.NET Core loads configuration and registers the EF Core context, Identity services, authentication, and application services.
3. Outside Development, the application configures exception handling, HSTS, HTTPS redirection, and durable data-protection storage.
4. The website accepts requests; database-backed operations require the externally provisioned SQL Server to be reachable.

No readiness wait mechanism or container dependency chain is declared in the target workspace.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `ConnectionStrings:DefaultConnectionString` / `SQLCONNSTR_DefaultConnectionString` | Database connection string | `[MASKED — supplied by host]` |
| `Authentication:{Provider}:ClientSecret` | OAuth client secret | `[MASKED — required only for enabled provider]` |
| `MachineLearning:AccountKey` | External API credential | `[MASKED — required only when recommendations are enabled]` |
| `DataProtection:KeyPath` | Key-ring storage location, not a key value | `[HOST-PROVIDED PATH]` |

### Secrets Provisioning Workflow

The active application reads database, OAuth, and optional recommendation credentials from host configuration. The repository contains no active plaintext credential values. A `Security/ExternalSecretConfiguration.cs` helper mentions Key Vault and managed identity, but the active project file excludes that source file from compilation; no active Key Vault provisioning chain was identified.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| `ShowRecommendations` | `false` | Application configuration |
| `Authentication:Facebook:Enabled` | `false` when absent | Application configuration |
| `Authentication:Google:Enabled` | `false` when absent | Application configuration |
| `Authentication:Microsoft:Enabled` | `false` when absent | Application configuration |
| `Authentication:Twitter:Enabled` | `false` when absent | Application configuration |

No dedicated feature-management framework or remote flag service was identified.

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET target framework | `net10.0` | Website `.csproj` |
| .NET runtime framework | `10.0.13` | Website `.csproj` |
| ASP.NET Core Identity | `10.0.0` | Website package references |
| Entity Framework Core SQL Server | `10.0.0` | Website package references |
| Newtonsoft.Json | `13.0.3` | Website package references |
| SQL project schema provider | SQL Server 130 | Database `.sqlproj` |
| Test framework | Not assessed in target workspace | Separate repository tests are out of scope |
