# PartsUnlimited Assessment Report

**Generated:** 2026-11-09T10:46:38Z  
**Assessment scope:** `IaaS2PaaSWeb`

> **Assessment limitation:** AppCAT could not complete because the legacy ASP.NET project requires `Microsoft.WebApplication.targets`, which is unavailable in this Linux environment. This report does not contain AppCAT migration findings; the security and supplementary architecture assessments were completed separately.

## Summary

| Metric | Count |
|---|---:|
| Total Issues | 5 |
| CVE Vulnerabilities meeting critical threshold | 0 |
| CWE Vulnerabilities | 5 |
| Mandatory | 0 |
| Potential | 3 |
| Optional | 2 |

## Application Information

| Property | Value |
|---|---|
| Application | PartsUnlimited |
| Language | C# |
| Runtime | .NET Framework 4.5.1 |
| Web frameworks | ASP.NET MVC 5.2.3; ASP.NET Web API 2 |
| Database | SQL Server |
| Assessment result | Security-only report; AppCAT analysis was blocked |

## Security Issues

### Hard-coded Password

- **Rule:** CWE-259
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story points:** 5
- **Files:** `IaaS2PaaSWeb/PartsUnlimitedWebsite/Web.config:12`, `IaaS2PaaSWeb/PartsUnlimitedWebsite/App_Start/Startup.Auth.cs:185-193`

A literal administrator password is configured and read by startup code when creating the administrator account. The configured value is intentionally not reproduced in this report.

### Use of Hard-coded Credentials

- **Rule:** CWE-798
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story points:** 5
- **Files:** `IaaS2PaaSWeb/PartsUnlimitedWebsite/Web.config:12`, `IaaS2PaaSWeb/PartsUnlimitedWebsite/App_Start/Startup.Auth.cs:185-193`

Startup code consumes the configured literal administrator password to create the application's administrator account. The configured value is intentionally not reproduced in this report.

### Insufficient Logging

- **Rule:** CWE-778
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story points:** 3
- **Files:** `IaaS2PaaSWeb/PartsUnlimitedWebsite/Controllers/AccountController.cs:48-76`

The login action handles successful, failed, locked-out, and verification-required outcomes without recording those authentication events in an audit or logging facility.

### Improper Control of Resource Identifiers

- **Rule:** CWE-99
- **Category:** Injection Attacks
- **Severity:** potential
- **Story points:** 3
- **Files:** `IaaS2PaaSWeb/PartsUnlimitedWebsite/Controllers/RecommendationsController.cs:22`, `IaaS2PaaSWeb/PartsUnlimitedWebsite/Recommendations/AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs:35`

The request-supplied product identifier is interpolated into an outbound recommendation-service query URI without restriction or encoding, so query delimiters in the input can affect the requested resource/query.

### Missing Release of Resource after Effective Lifetime

- **Rule:** CWE-772
- **Category:** Code Quality
- **Severity:** potential
- **Story points:** 3
- **Files:** `IaaS2PaaSWeb/PartsUnlimitedWebsite/App_Start/Startup.Auth.cs:179`

Startup code creates a disposable Entity Framework context for a role store without disposing the context or its owning role manager after the operation.

No CVE findings met the required critical severity threshold. The CVE scan found lower-severity advisories, which were excluded from the merged report by that threshold.

## Codebase Insights

- [Architecture diagram](facts/architecture-diagram.md)
- [Dependency map](facts/dependency-map.md)
- [API and service contracts](facts/api-service-contracts.md)
- [Data architecture](facts/data-architecture.md)
- [Configuration inventory](facts/configuration-inventory.md)
- [Business workflows](facts/business-workflows.md)
- [Assessment overview](facts/assessment-overview.md)
