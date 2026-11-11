# Configuration & Externalized Settings Inventory

The website's runtime settings are primarily stored in `Web.config`, with Debug and Release transform files and separate Azure/PowerShell deployment configuration. Several integration credentials are configuration-backed; the checked-in administrator password setting is nonempty and is masked here.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| Website application configuration | .NET XML configuration | `PartsUnlimitedWebsite/Web.config` | App settings, connection string, Entity Framework provider, and ASP.NET runtime settings |
| Debug transform | XML transform | `PartsUnlimitedWebsite/Web.Debug.config` | Build/deployment-specific transform |
| Release transform | XML transform | `PartsUnlimitedWebsite/Web.Release.config` | Applies Release-specific error handling and removes selected debug settings |
| Project and package manifests | MSBuild/NuGet | `PartsUnlimitedWebsite/PartsUnlimitedWebsite.csproj`, `PartsUnlimitedWebsite/packages.config` | Target framework, build configurations, assembly references, and package versions |
| Solution and deployment projects | Visual Studio/MSBuild | `IaaS2PaaSWeb.sln`, `Environments/Environments.deployproj`, `PartsUnlimitedDatabase/PartsUnlimitedDatabase.sqlproj` | Web, database, and environment deployment definitions |
| Environment deployment configuration | JSON and PowerShell/DSC | `Environments/*.json`, `Environments/*.parameters.json`, `Environments/*.ps1`, `Environments/DSC/*.ps1` | Azure resource and machine configuration; parameter files externalize deployment inputs |
| Publish profile | Publish settings | `PartsUnlimitedWebsite/Properties/PublishProfiles/FolderProfile.pubxml` | Folder deployment profile |

No Spring profiles, `appsettings.{Environment}.json`, `.env` files, remote configuration server, Key Vault, Vault, or AWS Secrets Manager integration was identified.

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | MSBuild `Debug` configuration | Local/debug build; defines `TRACE`, `DEBUG`, and `DEMO` | Classic .NET Framework Web Application build |
| Release | MSBuild `Release` configuration | Optimized deployment build; defines `TRACE` | Uses `Web.Release.config` transform |

The project is an older non-SDK-style C# Web Application targeting .NET Framework 4.5.1.

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | Website configuration loaded by System.Configuration | `Web.config` | SQL Server LocalDB connection and application settings |
| Debug | Deployment transform | `Web.Debug.config` | Debug deployment transform |
| Release | Deployment transform | `Web.Release.config` | Remote-only custom errors; removes the `debug` compilation attribute |

There are no ASP.NET Core environment profiles or separate application-level database profiles. The included transform files do not declare a separate production database connection.

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `ImagePath` | `/Images` | Default | `Web.config` |
| `ShowRecommendations` | `false` | Default | `Web.config`; controls whether product recommendations are shown |
| `SiteDelayMilliseconds` | `0` | Default | `Web.config`; read by the store controller |
| `Authentication.Administrator.UserName` | Configured administrator account | Default | `Web.config`; used by startup user seeding |
| `Authentication.Administrator.Password` | `[MASKED — nonempty configured value]` | Default | `Web.config`; read by startup user seeding |
| `Authentication.{Facebook,Google,Microsoft,Twitter}.Key` | Empty or placeholder | Default | `Web.config`; external provider client IDs |
| `Authentication.{Facebook,Google,Microsoft,Twitter}.Secret` | Empty | Default | `Web.config`; external provider secrets |
| `MachineLearning.ModelName` | Configured model name | Default | `Web.config`; used to build the Azure ML request |
| `MachineLearning.AccountKey` | Empty | Default | `Web.config`; no populated key was identified |
| `Keys:ApplicationInsights:InstrumentationKey` | `[MASKED]` | Default | `Web.config`; telemetry integration is not active in the inspected provider |
| `DefaultConnectionString` | SQL Server LocalDB with integrated authentication | Default | `Web.config`; raw connection string omitted |
| `entityFramework` provider/factory | SQL Client and LocalDB factory | Default | `Web.config` |
| `customErrors` | Off | Default; Release transform changes behavior | `Web.config`, `Web.Release.config` |
| `compilation.debug` | `true` | Default; removed by Release transform | `Web.config`, `Web.Release.config` |
| `compilation.targetFramework` / `httpRuntime.targetFramework` | `4.5.1` | Default | `Web.config` |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| PartsUnlimitedWebsite | .NET Framework 4.5.1 under IIS/IIS Express; no JVM options | Not specified | Not specified |
| SQL Server LocalDB | LocalDB connection configured by the website | Not specified | Development/local instance |

The deployment templates configure Windows and SQL Server resources, but no per-container CPU/memory limits or application instance count were found in the website project.

## Startup Dependency Chain

1. IIS/IIS Express loads the website and its System.Web configuration.
2. OWIN and Unity initialize authentication and the application's dependency registrations.
3. Database-backed actions require the configured SQL Server database; EF6 migrations/initializer support schema setup and seed data.
4. Azure ML recommendations and external OAuth providers are contacted only when their features are used; they are not required for application startup.

No application health-check wait mechanism, readiness endpoint, or configured startup retry for the database was identified.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `Authentication.Administrator.Password` | Administrator password | Checked-in `Web.config`; `[MASKED]` |
| `Authentication.{Facebook,Google,Microsoft,Twitter}.Secret` | OAuth client secrets | `Web.config`; currently empty |
| `MachineLearning.AccountKey` | External service key | `Web.config`; currently empty |
| `Keys:ApplicationInsights:InstrumentationKey` | Telemetry identifier | `Web.config`; `[MASKED]` |
| Deployment administrator/database credentials | Deployment inputs | Parameter/script variables; values are not reproduced here |

### Secrets Provisioning Workflow

The application reads configuration values through `ConfigurationManager` and `ConfigurationHelpers`. OAuth and machine-learning credentials are referenced by configuration key, but the inspected provider settings are empty or placeholders. The administrator password setting is populated in the checked-in `Web.config` and is consumed by startup seeding; no secret vault, encryption-at-rest for configuration, rotation process, or managed-identity workflow was identified. Deployment scripts accept credential variables/parameters, but their values are not included in this inventory.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| `ShowRecommendations` | `false` | `Web.config` app setting |
| `DEMO` conditional compilation symbol | Enabled in Debug only | MSBuild Debug configuration |
| Application Insights telemetry integration | Disabled/not active in inspected implementation | Telemetry provider/startup code |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework | 4.5.1 | `PartsUnlimitedWebsite.csproj`, `Web.config` |
| ASP.NET MVC | 5.2.3 | `packages.config` |
| ASP.NET Web API | 5.2.3 | `packages.config` |
| Entity Framework | 6.1.3 | `packages.config` |
| ASP.NET Identity | 2.2.1 | `packages.config` |
| Microsoft.Owin | 3.1.0 | `packages.config` |
| ASP.NET SignalR | 2.2.2 | `packages.config` |
| Newtonsoft.Json | 10.0.3 | `packages.config` |
| Unity | 4.0.1 | `packages.config` |
| Swashbuckle | 5.6.0 | `packages.config` |
| MSBuild project format | ToolsVersion 12.0, non-SDK-style | `PartsUnlimitedWebsite.csproj` |
| Hosting OS/runtime | IIS/IIS Express on Windows for the classic Web Application targets | Project and environment deployment files |
