# Azure App Service readiness assessment

Assessment date: 2026-10-23

Repository: Caldova-Asset-Manager

> Historical assessment of the former .NET Framework application. The active
> website now targets .NET 10 / ASP.NET Core. Do not apply its System.Web,
> framework build-tool, LocalDB or startup-seeding findings to the current
> project. See the [current readiness plan](../.github/modernize/plans/app-service/plan.md)
> and [Windows App Service deployment runbook](AZURE-APP-SERVICE-DEPLOYMENT.md).

## Verdict

**Conditionally suitable for Azure App Service on Windows; not ready for production deployment unchanged.**

The repository contains a CaldovaAssetManager ASP.NET MVC 5 / Web API application using System.Web, OWIN, Entity Framework 6, ASP.NET Identity, and classic ASP.NET SignalR. The website targets .NET Framework 4.5.1. Windows App Service is the lowest-change hosting destination. Linux App Service cannot run this application unchanged; that path requires migration to modern .NET and ASP.NET Core.

This is a source-based assessment, not a successful AppCAT scan or a deployment certification. No numerical readiness score is assigned because automated analysis did not complete.

## Assessment execution and limitations

- Ran the GitHub Copilot modernization .NET environment preparation and assessment tools against the repository root.
- Preparation reported .NET SDK and AppCAT ready; subsequently verified SDK 10.0.402. The preparation tool reported AppCAT 1.0.1127.58951.
- AppCAT failed in the cloud-readiness domain while loading the website project. The installed Visual Studio Build Tools 2019 instance lacks `Microsoft.WebApplication.targets`.
- The project imports that target at [Caldova-Asset-Manager.csproj](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj#L547).
- Checked installed Visual Studio instances and the installed .NET SDK for an alternative web application target; none was found.
- Failure details are retained in the [AppCAT log](../.github/modernize/assessment/copilot/engines/dotnet-appcat/appcat.log). The generated [report shell](../.github/modernize/assessment/copilot/reports/report-20261023200453/report.json) has empty projects, rules, and summary, an unfinished status, and a placeholder end time. It is not a completed assessment and must not be interpreted as zero findings.
- The generated assessment directory is ignored by the current Git configuration. This document preserves the findings independently of those local outputs.
- No application code was changed, Azure resources created, or application deployed. No build, runtime, live database compatibility, network connectivity, load, or failover test completed.
- No vulnerability scan completed. Dependency age is noted below, but no CVE counts or security certification are claimed.

## Findings and required actions

| Priority | Area | Evidence | Readiness impact and action |
| --- | --- | --- | --- |
| Required | Windows hosting | [Website project](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj#L17), [ASP.NET application entry point](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Global.asax.cs) | Use Windows App Service with the supported .NET Framework 4.x runtime. System.Web is not supported by Linux App Service. A modern .NET rewrite is not a prerequisite for a Windows-hosted migration. |
| Required for supported production baseline | Framework lifecycle | [Website project](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj#L17), [runtime configuration](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config) | .NET Framework 4.5.1 is out of support. Retarget to a supported .NET Framework version, preferably 4.8 after confirming the chosen App Service environment, and regression-test. App Service's newer in-place 4.x runtime may run older binaries, but that does not make the old target a supported or validated production baseline. |
| Required | Database connectivity | [Connection string and EF defaults](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config), [database context](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Models/CaldovaAssetManagerContext.cs) | The checked-in connection uses LocalDB and Windows integrated security. It is not a usable App Service production database configuration. Provision Azure SQL Database or establish connectivity to an existing supported SQL Server. Override the exact connection-string name `DefaultConnectionString` in App Service. Validate TLS, authentication, firewall/private connectivity, and schema/data migration. |
| Required | Database provisioning | [Application startup](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Global.asax.cs#L22), [initializer](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/CaldovaAssetManagerDbInitializer.cs#L11), [SQL creation script](../IaaS2PaaSWeb/Caldova-Asset-Manager.Database/Pre-Deployment/01_Create_Database.sql) | Startup uses `CreateDatabaseIfNotExists` with sample-data seeding. Pre-provision the database and deploy schema/data through a controlled migration identity; do not depend on the web app creating the production database. Review scripts in their intended execution context, rather than running database-creation scripts against an already-connected application database. |
| Required | Administrator provisioning | [Administrator configuration](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config), [startup account creation](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/App_Start/Startup.Auth.cs#L177) | Startup creates an administrator and role from configuration when absent. Replace the checked-in demonstration credential and move sensitive settings outside source control. Prefer controlled, one-time provisioning. Validate parallel-start behavior and keep ongoing application database permissions minimal. No credential values are reproduced here. |
| Required | Repeatable build and publish | [Legacy project imports](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj#L546), [NuGet manifest](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/packages.config), [solution](../IaaS2PaaSWeb/Caldova-Asset-Manager.sln) | Restore packages and build on a Windows agent with ASP.NET web build targets and the matching framework reference assemblies. Publish the website project specifically as a Release artifact. Do not assume the .NET SDK alone can build this legacy solution. |
| Required | Infrastructure definition | [Workshop instructions](../README.md), [web VM template](../IaaS2PaaSWeb/Environments/nestedtemplates/WebSrv.json#L102), [SQL VM template](../IaaS2PaaSWeb/Environments/nestedtemplates/SqlSrv.json#L83) | Existing templates deploy VMs, not an App Service application. Create a separate App Service plan, Windows web app, database/network configuration, settings, monitoring, and deployment workflow. Do not run the workshop deployment as an App Service migration. |
| Required before production | Dependency maintenance | [NuGet packages](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/packages.config) | Packages include EF 6.1.3, MVC 5.2.3, OWIN 3.1.0, SignalR 2.2.2, and Newtonsoft.Json 10.0.3. Run a current dependency vulnerability assessment, update supported versions, and regression-test. Exact vulnerabilities remain unassessed. |
| Required before scale-out; relevant to restarts | Session state | [Shopping cart ID](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Models/ShoppingCart.cs#L164), [web configuration](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config) | Cart identity is in ASP.NET session. There is no external session provider configured, so it defaults to process-local state. Restart or routing to another instance can lose access to an anonymous cart even though cart rows are stored in SQL. Decide on durable/shared session or a durable cart identifier. ARR affinity can help routing temporarily but does not preserve session across restarts. |
| Required before scale-out | Real-time updates | [SignalR registration](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/App_Start/Startup.Auth.cs#L111), [SignalR dependency resolver](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/SignalRDependencyResolver.cs) | Classic ASP.NET SignalR is registered without a visible scale-out configuration. Validate transports, enable WebSockets if used, and select a compatible shared message/backplane solution before multiple instances. Do not assume the existing classic SignalR package already integrates with Azure SignalR Service. |
| Required before production | Release configuration and observability | [Release transform](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.Release.config), [telemetry provider](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/TelemetryProvider.cs), [startup](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Startup.cs) | The Release transform removes debug compilation and changes detailed-error handling; ensure it is actually applied. Telemetry methods are commented out, so the configured instrumentation setting alone does not prove monitoring works. Configure and verify Application Insights or equivalent logging, HTTPS-only access, health checks, and alerts. |
| Conditional | External recommendation service | [Recommendation implementation](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Recommendations/AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs), [DI registration](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/App_Start/UnityConfig.cs#L18), [feature guard](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Controllers/RecommendationsController.cs) | The registered implementation calls a legacy Azure DataMarket endpoint. Recommendations are disabled by the checked-in `ShowRecommendations=false` setting. Keep disabled until a supported replacement and credentials are validated; endpoint availability was not tested. |
| Favorable, with verification | Static content and storage | [Image URL helper](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/HtmlHelperExtensions.cs), [image configuration](../IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Web.config) | Images appear to be deployed static content. The focused C# review found no runtime file-upload or local-file-write usage. Blob Storage is not established as a mandatory migration dependency. Verify all required content is included in the publish artifact. |

## Recommended initial target

- Windows Azure App Service using the supported .NET Framework 4.x runtime, after retargeting and compatibility testing.
- Azure SQL Database, subject to schema/data compatibility validation, or an existing supported SQL Server reachable through appropriately configured networking.
- App Service application settings and connection strings; Key Vault references where appropriate for secrets.
- Monitoring with verified request, exception, dependency, and availability telemetry.
- A Windows build/publish pipeline targeting only the website project, with controlled database migration as a separate step.
- Begin validation with one application instance. Do not treat that as an availability or session-durability solution; resolve state and real-time messaging before scaling.

Managed identity is desirable for database connectivity, but is not already implemented. Confirm the selected SQL client/provider supports the intended authentication mode and implement/test that integration; enabling an App Service identity alone is insufficient.

App Service tier, region, networking, sizing, and cost cannot be selected reliably from source alone. These depend on workload, availability requirements, database size, compliance, and existing Azure resources.

## Ordered next steps and acceptance gates

1. **Unblock automated analysis:** install/modify Visual Studio Build Tools with the ASP.NET/web build tooling and required reference assemblies. This may require administrator approval. Retain the original AppCAT failure evidence and rerun the cloud-readiness assessment. A valid result must contain completed metadata and the analyzed website project, not just an empty report shell.
2. **Establish a supported build:** retarget the framework, restore packages, update required dependencies, and produce a clean Release website publish artifact on a Windows build agent. Do not bypass missing targets by suppressing project imports.
3. **Validate database migration:** assess the actual source database, deploy schema/data to a test destination, configure `DefaultConnectionString`, and verify SQL authentication and connectivity. The standalone ReadyRoll/SQL Server 2016 database project is outside the current solution; validate its toolchain and SQL compatibility separately.
4. **Prepare production configuration:** replace demonstration administrator settings, control provisioning, verify Release transformations, configure secrets, TLS, diagnostics, and database least privilege.
5. **Deploy to a nonproduction App Service environment:** validate startup, database access, product browsing/search, account registration/login, administrator actions, cart persistence, checkout/order creation, and live announcements. Keep recommendations disabled unless separately validated.
6. **Test resilience:** restart the application, change instance routing, and test at least two instances after implementing shared state/messaging. Verify anonymous cart identity, authenticated cookies, and announcements. Exercise database transient-failure behavior and deployment rollback.
7. **Approve production readiness only after evidence:** successful automated assessment, reproducible build, database validation, functional and resilience checks, monitoring, and a reviewed deployment/rollback plan.

## Microsoft references

- [Configure ASP.NET Framework applications in App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnet-framework)
- [Deploy an ASP.NET application with Azure SQL Database](https://learn.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-dotnet-sqldatabase)

These references establish platform capabilities; they do not replace validation of this application.
