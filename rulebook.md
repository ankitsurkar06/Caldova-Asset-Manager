---
name: caldova-modernization-rulebook
description: Caldova's organization-wide modernization rulebook. Apply these rules whenever you assess, plan, upgrade or migrate any Caldova application to Azure, including .NET upgrades, Java upgrades, Oracle to PostgreSQL database migrations, secret and configuration management, security hardening, containerization, testing and observability. These rules are mandatory and take precedence over default modernization choices.
---

# Caldova Modernization Rulebook

## Purpose

This rulebook sets out how every Caldova application is modernized and moved to Azure. It applies to every modernization plan, upgrade and migration, whether a person or the GitHub Copilot modernization agent does the work.

The rules have three goals:

- Every modernized application runs on a supported, long-term-support platform.
- Security is built in from the start. No secrets in code, no passwords in configuration and no exposed endpoints.
- Every Caldova application looks and behaves the same way, so any team can support any application.

Where a rule says **must**, it is mandatory and cannot be skipped without a written exception from the Caldova Architecture Review Board. Where a rule says **should**, it is the expected default; if you don't follow it, say why in the modernization plan.

---

## Part 1: General rules for every application

These rules apply to every application, whatever its language, framework or database.

### 1.1 How modernization work is planned

- Every application must be assessed before any code changes. The assessment report must be saved in the repository with the plan.
- The modernization plan must list every task in order, the reason for each one and the rule in this rulebook it satisfies.
- Upgrade the framework first, then migrate the database, then move to the cloud. Never combine all three in one step.
- Each task must leave the application in a state that builds and passes its tests. Never hand over a half-migrated application.
- Each task must be committed separately, with a clear commit message that describes what changed and why.
- All work must happen on a dedicated modernization branch, never directly on the main branch.
- No change goes to production without a pull request reviewed by at least one person from the owning team.

### 1.2 Secret management with Azure Key Vault

This is the most important section of the rulebook. Caldova does not accept any exception to it.

- Every secret must be stored in Azure Key Vault. This covers passwords, connection strings that hold credentials, API keys, client secrets, certificates, signing keys, storage account keys and tokens.
- Secrets must never appear in source code, configuration files, environment variable files, container images, deployment scripts, pipeline definitions, test data or documentation.
- Applications must read secrets from Key Vault at runtime using a managed identity. They must never use a stored client secret to sign in to Key Vault itself.
- Each application must use a user-assigned managed identity, so access survives redeployment of the compute resource.
- Applications must be granted access through Azure role-based access control, never through legacy access policies. The application identity gets only the "Key Vault Secrets User" role, which can read secrets but not change them.
- Each application environment (development, test, production) must have its own Key Vault. Production secrets must never be reachable from a non-production environment.
- Key Vaults must have soft delete and purge protection turned on.
- Key Vaults must be reached over a private endpoint, with public network access turned off in production.
- Wherever the target Azure service supports Microsoft Entra ID authentication, the application must use its managed identity instead of a secret. Azure SQL, Azure Database for PostgreSQL, Azure Storage, Azure Service Bus, Azure Event Hubs and Azure Cosmos DB all support this. The most secure secret is the one that doesn't exist.
- Secrets must be rotated at least every 90 days, or sooner if the owning service requires it. Rotation must not need a code change or redeployment.
- Certificates must be stored in Key Vault as certificates, not as secrets, and must be set to renew automatically.
- Secret names must be descriptive and consistent, for example "OrdersDb-Password" or "PaymentGateway-ApiKey".
- If the assessment finds a secret anywhere in the code or its history, it must be treated as leaked. The secret must be rotated immediately and moved to Key Vault, and the incident must be reported to the security team.

### 1.3 Configuration management

- Settings that are not secret, such as feature flags, URLs, timeouts and limits, must be stored in Azure App Configuration or in environment-specific application settings.
- Configuration must never be hard-coded in application logic.
- An application must be able to move between environments by changing configuration only, never by changing code.
- Values that differ between environments must never be checked in to the default configuration files.
- Feature flags should be managed through Azure App Configuration so features can be turned on or off without a redeployment.

### 1.4 Coding standards

- Code must be readable first. Choose clear names over clever ones.
- Classes, methods and variables must have meaningful names that describe their purpose. Avoid abbreviations unless they are widely understood.
- Methods should do one thing. A method longer than about 50 lines should be split.
- Each class should have one clear responsibility.
- Duplicated logic must be refactored into a shared method or component.
- Dead code, commented-out code and unused dependencies must be removed during modernization.
- Every public method that calls a database, network service or file system must handle errors explicitly. Errors must never be silently swallowed.
- Calls to external services must use timeouts, retries with back-off, and circuit breakers where appropriate.
- Input from users or other systems must be validated before it is used.
- All SQL must use parameterized queries. Never build SQL by joining strings.
- Code formatting and style rules must be enforced automatically in the build, not left to individual developers.
- Compiler and static-analysis warnings must be fixed, not suppressed. A suppression needs a comment that explains why.
- Comments should explain why something is done, not what the code does.
- All code, comments and commit messages must be in English.

### 1.5 Dependencies and open-source packages

- Every dependency must be on a supported version that receives security fixes.
- Any package with a known critical or high vulnerability must be upgraded or replaced before the application goes live.
- Packages that are deprecated, abandoned or no longer maintained must be replaced with a supported alternative.
- Packages must come only from approved, trusted feeds.
- Dependency versions must be pinned so builds are repeatable.
- Automated dependency scanning must run on every pull request.

### 1.6 Testing

- Existing tests must be kept and must pass after every modernization task.
- New or changed business logic must have unit tests.
- Every application must have at least one automated test that proves it starts up and can reach its database and key dependencies.
- After a database migration, tests must compare results between the old and new database to show they behave the same.
- Code coverage must not go down as a result of modernization.

### 1.7 Logging, monitoring and observability

- Every application must send logs, metrics and traces to Azure Monitor and Application Insights through OpenTelemetry.
- Logs must be structured, not free text, so they can be searched and filtered.
- Logs must never contain secrets, passwords, tokens, or personal or customer data.
- Every application must expose a health check endpoint that hosting platforms can use for liveness and readiness.
- Each request must carry a correlation ID so it can be traced across services.
- Alerts must be set up for failures, high latency and resource exhaustion before go-live.

### 1.8 Hosting and containers

- Preferred hosting, in order: Azure Container Apps, then Azure App Service, then Azure Kubernetes Service. Choose AKS only when the application needs features the other two don't offer.
- Applications should be containerized.
- Container images must be built from official, minimal, supported base images.
- Containers must run as a non-root user.
- Images must be stored in Azure Container Registry and scanned for vulnerabilities before deployment.
- Applications must be stateless. Session state, files and caches must move to managed services such as Azure Cache for Redis or Azure Blob Storage, never the local disk.
- Every application must handle a graceful shutdown.

### 1.9 Network and platform security

- All traffic must use HTTPS with TLS 1.2 or later. Plain HTTP must be redirected or blocked.
- Databases, Key Vaults, storage accounts and message brokers must be reached over private endpoints in production.
- Public access to data services must be turned off in production.
- Applications must use Microsoft Entra ID for user sign-in and service-to-service authentication. Custom authentication code must be replaced.
- Applications must follow least privilege. Every identity gets only the permissions it needs.
- Security headers must be set on every web application, and cross-origin access must be limited to known origins.

### 1.10 Infrastructure as code and delivery

- All Azure resources must be defined as infrastructure as code using Bicep. Terraform is acceptable where a team already uses it.
- Nothing may be created by hand in the Azure portal for production.
- Every application must have a CI/CD pipeline that builds, tests, scans and deploys it.
- Pipelines must sign in to Azure with workload identity federation, not stored secrets.
- Every Azure resource must carry the Caldova tags: application name, owner, environment and cost center.

---

## Part 2: .NET modernization rules

These rules apply to every Caldova application written in C# or another .NET language.

### 2.1 Target platform

- All .NET applications must be upgraded to .NET 10, the current long-term-support release.
- .NET Framework applications (version 4.x and earlier) must be moved to modern .NET. Staying on .NET Framework needs an approved exception.
- Applications on .NET 8 or earlier must be upgraded to .NET 10 before .NET 8 reaches end of support.
- Use the latest C# language version that ships with .NET 10.

### 2.2 Project structure

- All projects must use the modern SDK-style project format.
- Shared build settings, such as target framework, nullable settings and warning rules, must be defined once for the whole solution, not repeated in each project.
- Package versions must be managed centrally for the whole solution.
- Old package configuration files must be removed and converted to package references.

### 2.3 Framework migration

- ASP.NET Web Forms applications must be rewritten as ASP.NET Core Razor Pages, MVC or Blazor. Web Forms cannot be upgraded in place.
- ASP.NET MVC and Web API applications must be moved to ASP.NET Core.
- WCF services must be moved to ASP.NET Core Web API (for REST) or gRPC. CoreWCF is allowed only as a temporary step, with a plan to move off it.
- Windows services must be moved to .NET Worker Services.
- Configuration in web.config and app.config must be moved to the modern .NET configuration system, with secrets moved to Azure Key Vault.
- Entity Framework 6 must be upgraded to Entity Framework Core.
- Dependency injection must use the built-in .NET dependency injection container.
- Windows-only features such as the registry, the Windows event log and local file shares must be replaced with cloud services.

### 2.4 .NET coding standards

- Nullable reference types must be turned on for all projects.
- Warnings must be treated as errors in release builds.
- Code analyzers must be turned on and set to the recommended level.
- All input and output operations, such as database, HTTP and file calls, must be asynchronous from top to bottom. Blocking on asynchronous code is not allowed.
- HTTP clients must be created through the HTTP client factory, never one per request.
- Logging must use the standard .NET logging interface with structured messages.
- Settings must be read through strongly typed options classes, not by looking up raw configuration keys throughout the code.
- Code style must follow the shared Caldova code style file in the repository root.

### 2.5 .NET secret and identity rules

- Applications must load Key Vault secrets into the .NET configuration system at startup, using a managed identity.
- Applications must use the Azure Identity library's default credential, so the same code works on a developer's machine and in Azure without changes.
- Developers must use their own Microsoft Entra sign-in for local development. User secrets are allowed only for local-only values that will never be deployed.
- Connection strings to Azure SQL and Azure Database for PostgreSQL must use Microsoft Entra authentication with no password.

### 2.6 .NET data access

- Entity Framework Core is the default data access technology. Dapper is allowed for performance-critical read paths.
- Database schema changes must be managed through migrations held in source control.
- Connection resilience and retry must be turned on for every cloud database connection.

---

## Part 3: Java modernization rules

These rules apply to every Caldova application written in Java.

### 3.1 Target platform

- All Java applications must run on Java 21 or Java 25, both long-term-support releases. Java 25 is preferred for new work.
- Applications on Java 8, 11 or 17 must be upgraded.
- Only supported OpenJDK builds may be used. The Microsoft Build of OpenJDK is preferred.

### 3.2 Framework migration

- Spring Boot applications must be upgraded to a supported Spring Boot 3.x release or later. Spring Boot 2.x is out of support and must not be used.
- Code using the old javax namespace must be moved to the Jakarta EE namespace.
- Applications on traditional application servers such as WebLogic, WebSphere or JBoss should be moved to Spring Boot or another lightweight runtime. If the application must keep an application server, it must move to a supported version and run in a container.
- Proprietary application server features such as server-managed connection pools, message queues and naming lookups must be replaced with portable alternatives.
- Message queues such as RabbitMQ, ActiveMQ or IBM MQ should be moved to Azure Service Bus.
- Local file storage must be moved to Azure Blob Storage.
- Scheduled jobs must not depend on a single server. Use a platform scheduler or a distributed scheduling approach.

### 3.3 Build and project structure

- Builds must use Maven or Gradle with a wrapper checked in, so every developer and pipeline uses the same build tool version.
- Dependency versions must be managed in one place, using a bill of materials where available.
- Builds must fail on known critical or high vulnerabilities.
- Unused dependencies must be removed.

### 3.4 Java coding standards

- Code must follow the Caldova Java style guide, enforced automatically in the build.
- Static analysis must run on every build, and new issues must be fixed before merge.
- Use modern language features where they make code clearer, such as records for simple data carriers.
- Use constructor injection, not field injection.
- Use SLF4J for logging with structured output.
- Never catch generic exceptions without logging and re-throwing or handling them properly.
- Resources such as connections, streams and files must always be closed reliably.
- Use virtual threads for high-concurrency input and output work on Java 21 and later, where the framework supports them.

### 3.5 Java secret and identity rules

- Spring applications must read secrets from Azure Key Vault through Spring Cloud Azure, using a managed identity.
- Database connections to Azure Database for PostgreSQL and Azure SQL must use Microsoft Entra authentication with no password.
- Credentials must never be put in application properties or YAML files, even for test environments.
- Local development must use the developer's own Azure sign-in, not shared credentials.

### 3.6 Java data access

- Spring Data JPA with Hibernate is the default data access approach.
- Database schema changes must be managed with Flyway or Liquibase, held in source control.
- Connection pools must be sized for the target environment and must use retry on transient failures.

---

## Part 4: Oracle to PostgreSQL modernization rules

These rules apply when a Caldova application's Oracle database is being moved to PostgreSQL.

### 4.1 Target platform

- The target database must be Azure Database for PostgreSQL Flexible Server.
- Use PostgreSQL 16 or later.
- Production databases must have zone-redundant high availability turned on.
- Automated backups must be kept for at least 14 days in production.
- Public network access must be turned off in production. The database must be reached through a private endpoint or virtual network integration.
- Applications must connect using Microsoft Entra authentication with a managed identity. If a password-based database account is unavoidable, its password must be stored in Azure Key Vault.

### 4.2 Assessment before migration

- Every Oracle database must be assessed before migration. The assessment must cover schema objects, PL/SQL code, data volumes, Oracle-specific features in use and how the application uses the database.
- The assessment must list every package, procedure, function, trigger, view, materialized view, sequence, synonym and database link.
- Each object must be rated by how hard it is to convert, and the plan must call out anything that needs a redesign rather than a straight conversion.
- Use approved tooling such as Ora2Pg for the assessment and the first pass of schema conversion. Every converted object must still be reviewed by a person.
- SQL embedded in application code must be found and listed as part of the assessment, not just the code in the database.

### 4.3 Schema conversion

- Object names must be converted to lower case. Avoid quoted identifiers, because PostgreSQL treats them as case-sensitive.
- Oracle schemas should map to PostgreSQL schemas inside one database, one for each application area.
- Data types must be mapped carefully:
  - Oracle NUMBER columns must map to the most precise matching type: integer or bigint for whole numbers, and numeric with the right precision for decimals. Don't map every NUMBER to numeric without thinking.
  - VARCHAR2 and NVARCHAR2 map to varchar or text.
  - Oracle DATE holds a time as well as a date, so it must map to timestamp, not to the PostgreSQL date type, unless the time part is confirmed as unused.
  - CLOB maps to text, and BLOB maps to bytea.
  - RAW maps to bytea, and Oracle GUID values should map to uuid.
  - Timestamp with time zone must keep its time zone information.
- Sequences used for primary keys should be converted to identity columns.
- Synonyms must be replaced by schema search paths or by qualifying object names.
- Database links must be removed. Cross-database access must be redesigned through APIs or proper data integration.
- Materialized views must be converted, with a defined refresh schedule.
- Partitioned tables must use PostgreSQL native declarative partitioning.
- Indexes must be reviewed, not copied blindly. Oracle bitmap and function-based indexes need specific handling.
- All primary keys, foreign keys, unique constraints and check constraints must be kept.

### 4.4 Code and SQL conversion

- PL/SQL procedures and functions must be converted to PL/pgSQL.
- Oracle packages have no direct match in PostgreSQL. Each package should become its own schema holding its functions and procedures. Package-level variables must be redesigned.
- Business logic should be moved out of the database and into the application where practical. Don't convert complex PL/SQL line by line if the logic belongs in application code.
- Oracle-specific SQL must be rewritten into standard SQL or PostgreSQL equivalents, including:
  - Outer joins written with the Oracle plus-sign syntax must use standard join syntax.
  - Hierarchical queries using CONNECT BY must use recursive common table expressions.
  - Row limits using ROWNUM must use standard row limiting.
  - NVL becomes COALESCE, and DECODE becomes a CASE expression.
  - Queries against the DUAL table must drop it.
  - SYSDATE and SYSTIMESTAMP must use PostgreSQL's current time functions, and teams must choose deliberately between transaction time and clock time.
- Oracle treats an empty string as NULL; PostgreSQL does not. Every comparison and constraint that depends on this must be reviewed and tested.
- Autonomous transactions are not supported in PostgreSQL and must be redesigned.
- Oracle optimizer hints must be removed. Performance must be addressed through indexes, statistics and query design.
- Error handling and exception names must be mapped to PostgreSQL error codes.
- Triggers must be converted to PostgreSQL trigger functions and reviewed to check they are still needed.
- Scheduled database jobs must be moved to the pg_cron extension or an application-level scheduler.

### 4.5 Application changes

- Oracle JDBC drivers and Oracle .NET data providers must be replaced with the PostgreSQL drivers: the PostgreSQL JDBC driver for Java and Npgsql for .NET.
- Data access frameworks must be switched to their PostgreSQL dialect or provider.
- SQL embedded in the application must be converted under the same rules as database code.
- Connection strings must be updated to use Microsoft Entra authentication and stored as configuration, with any credentials kept in Azure Key Vault.
- Connection pooling must be configured. Use the built-in PgBouncer on Flexible Server for applications with many short connections.

### 4.6 Data migration

- Data must be migrated with approved tooling. Choose an online migration with ongoing change capture for large or business-critical databases that can't afford long downtime.
- Character set and collation must be checked, and data must be moved as UTF-8.
- After migration, row counts must match for every table.
- Checksums or sample comparisons must confirm the data is correct, especially for numbers, dates and large text or binary columns.
- Sequences and identity columns must be reset to continue after the highest migrated value.
- Table statistics must be refreshed after the data load.

### 4.7 Testing and cut-over

- The full application test suite must pass against PostgreSQL before cut-over.
- Critical queries and reports must be run against both databases, and the results compared.
- Performance tests must show the application meets its agreed response times on PostgreSQL.
- A written cut-over plan must cover the downtime window, the final data sync, validation steps and who signs off.
- A rollback plan must exist and be tested before cut-over.
- The Oracle database must be kept read-only for an agreed period after cut-over, then retired, and its licenses released.

---

## Part 5: Definition of done

A modernization is complete only when all of the following are true:

- The application runs on the target platform version set out in this rulebook.
- No secrets exist anywhere in the code, configuration or pipeline. All secrets are in Azure Key Vault and are reached through a managed identity.
- All dependencies are supported and free of known critical or high vulnerabilities.
- The application builds with no warnings and passes all tests.
- Logs, metrics, traces, health checks and alerts are in place.
- All Azure resources are defined in infrastructure as code and tagged correctly.
- For database migrations, data has been validated and the cut-over and rollback plans are signed off.
- The modernization plan, assessment report and any approved exceptions are saved in the repository.

## Exceptions

Any request to depart from a **must** rule needs written approval from the Caldova Architecture Review Board. The approval must name the rule, the reason, the risk, the compensating controls and an expiry date. No exception is ever granted for storing secrets outside Azure Key Vault.
