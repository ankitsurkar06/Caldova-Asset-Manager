# Security Assessment Report

**Generated:** 2026-11-09T10:46:38.216370Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 5 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 5 |
| Total Rules Assessed | 59 |
| Rules Passed | 54 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 0 |
| optional | 2 |
| potential | 3 |

## CVE Findings (Dependency Vulnerabilities)

No CVE findings met the **critical** minimum severity threshold.

## CWE Findings (Code-Level Vulnerabilities)

### CWE-772: Missing Release of Resource after Effective Lifetime
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 3
- **Files:** IaaS2PaaSWeb/PartsUnlimitedWebsite/App_Start/Startup.Auth.cs:179

CreateAdminUser constructs a disposable PartsUnlimitedContext and passes it into a RoleStore, but neither the context nor the owning RoleManager is disposed after the startup operation. The context can retain database resources beyond the operation that created it.

### CWE-259: Use of Hard-coded Password
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** IaaS2PaaSWeb/PartsUnlimitedWebsite/Web.config:12, IaaS2PaaSWeb/PartsUnlimitedWebsite/App_Start/Startup.Auth.cs:185-193

A literal is configured for Authentication.Administrator.Password and Startup.Auth.CreateAdminUser reads it to create the inbound administrator account when that user is absent.

### CWE-778: Insufficient Logging
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story Points:** 3
- **Files:** IaaS2PaaSWeb/PartsUnlimitedWebsite/Controllers/AccountController.cs:48-76

AccountController.Login handles successful, failed, locked-out, and verification-required authentication outcomes but records none of these security events in an audit or logging facility; the failed attempt path only adds a user-facing model error.

### CWE-798: Use of Hard-coded Credentials
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** IaaS2PaaSWeb/PartsUnlimitedWebsite/Web.config:12, IaaS2PaaSWeb/PartsUnlimitedWebsite/App_Start/Startup.Auth.cs:185-193

A configured literal administrator password is consumed by CreateAdminUser to create the application administrator; this is a hard-coded credential in application configuration.

### CWE-99: Improper Control of Resource Identifiers ('Resource Injection')
- **Category:** Injection Attacks
- **Severity:** potential
- **Story Points:** 3
- **Files:** IaaS2PaaSWeb/PartsUnlimitedWebsite/Controllers/RecommendationsController.cs:22, IaaS2PaaSWeb/PartsUnlimitedWebsite/Recommendations/AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs:35

RecommendationsController.GetRecommendations accepts productId from the request and passes it to GetRecommendationsAsync. That value is interpolated directly into the external service URI query string at line 35 without restricting or encoding it, allowing query delimiters in the input to alter the requested resource/query.
