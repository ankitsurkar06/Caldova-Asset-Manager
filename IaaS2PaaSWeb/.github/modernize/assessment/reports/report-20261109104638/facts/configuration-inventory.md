# Configuration & Externalized Settings Inventory

Configuration is primarily stored in the website's `Web.config`, with build settings in legacy MSBuild project files and infrastructure settings in ARM templates and deployment scripts. One application configuration set is present; deployment-specific values are supplied outside the source-level runtime profiles.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| Web application configuration | XML | `PartsUnlimitedWebsite/Web.config` | App settings, named SQL connection, authentication provider settings and telemetry |
| View configuration | XML | `PartsUnlimitedWebsite/Views/Web.config` and area view configuration | MVC view behavior |
| Application project | MSBuild | `PartsUnlimitedWebsite/PartsUnlimitedWebsite.csproj` | .NET Framework target and Debug/Release settings |
| NuGet packages | XML | `PartsUnlimitedWebsite/packages.config` | Pinned package versions |
| SQL project | MSBuild | `PartsUnlimitedDatabase/PartsUnlimitedDatabase.sqlproj` | SQL schema deployment options |
| Infrastructure templates | ARM JSON | `Environments/WorkshopEnv.json`, `Environments/nestedtemplates/` | Web/SQL VM and network resources |
| Deployment scripts | PowerShell | `Environments/Deploy-AzureResourceGroup.ps1`, `Environments/CustomScripts/`, `Environments/DSC/` | Provisioning and configuration automation |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | MSBuild `Debug` configuration | Debug symbols and development compilation | Defines `TRACE`, `DEBUG`, and `DEMO` |
| Release | MSBuild `Release` configuration | Optimized release output | Defines `TRACE` |
| SQL Debug | SQL project `Debug` configuration | Builds database deployment artifacts | SQL project/ReadyRoll targets |
| SQL Release | SQL project `Release` configuration | Optimized database artifact | SQL project/ReadyRoll targets |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | IIS application configuration | `PartsUnlimitedWebsite/Web.config` | Single checked-in settings set; environment-specific override files were not identified |
| Azure deployment | ARM deployment parameters | `Environments/*.parameters.json` | Infrastructure and deployment parameter values |

## Properties Inventory

| Service | Property Key | Default / Source | Profiles |
|---|---|---|---|
| Website | `ImagePath` | `/Images` | Default |
| Website | `ShowRecommendations` | `false` | Default |
| Website | `SiteDelayMilliseconds` | `0` | Default |
| Website | `DefaultConnectionString` | Named connection; endpoint and credentials are not reproduced here | Deployment-defined |
| Website | `Authentication.Administrator.UserName` | Configured administrator account | Default |
| Website | `Authentication.Administrator.Password` | **Configured literal; value intentionally redacted** | Default |
| Website | `Authentication.*.Key` / `Authentication.*.Secret` | Provider settings; several are empty, configured values are redacted | Default |
| Website | `MachineLearning.ModelName` | `sampletest` | Default |
| Website | `MachineLearning.AccountKey` | Empty | Default |
| Website | Application Insights instrumentation key | Configured literal; value intentionally redacted | Default |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| PartsUnlimitedWebsite | IIS-hosted .NET Framework 4.5.1 application | Not specified in application configuration | Deployment-defined |
| PartsUnlimitedDatabase | SQL Server on deployment-defined infrastructure | VM sizing is determined by ARM parameters | Deployment-defined |

No JVM parameters or container resource limits were identified.

## Startup Dependency Chain

1. Azure deployment provisions network and virtual machine resources using the ARM templates.
2. The SQL Server VM and database schema must be available for database-backed website functions.
3. IIS hosts the website; no application-level database readiness probe or explicit startup wait mechanism was identified.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `Authentication.Administrator.Password` | Administrator password | Literal in `Web.config` (**redacted**) |
| `Authentication.Google.Key` | OAuth client identifier | Configured value in `Web.config` (**redacted**) |
| `Authentication.*.Secret` | OAuth provider secret | Empty values in the checked-in configuration |
| `MachineLearning.AccountKey` | External service key | Empty in the checked-in configuration |
| Application Insights instrumentation key | Telemetry configuration | Configured value in `Web.config` (**redacted**) |
| `DefaultConnectionString` | Database connection | Named configuration; value not reproduced |

### Secrets Provisioning Workflow

No managed identity, secret store, or automated secret-injection workflow was identified in the application configuration. The administrator password is read from the application settings during startup to create the administrator account. External provider credentials and the SQL connection must be supplied through deployment configuration; no source-level secure provisioning sequence is documented.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| `ShowRecommendations` | `false` | `Web.config` app setting |
| `SiteDelayMilliseconds` | `0` | `Web.config` app setting |
| `DEMO` compilation symbol | Enabled for Debug | MSBuild Debug configuration |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework | 4.5.1 | `PartsUnlimitedWebsite.csproj` |
| ASP.NET MVC | 5.2.3 | `packages.config` |
| ASP.NET Web API | 5.2.3 | `packages.config` |
| Entity Framework | 6.1.3 | `packages.config` |
| ASP.NET Identity | 2.2.1 | `packages.config` |
| OWIN | 3.1.0 | `packages.config` |
| ASP.NET SignalR | 2.2.2 | `packages.config` |
| Newtonsoft.Json | 10.0.3 | `packages.config` |
| SQL project | SQL130 schema provider | `PartsUnlimitedDatabase.sqlproj` |
| Build tooling | MSBuild / Visual Studio solution format | `.sln` and project files |
