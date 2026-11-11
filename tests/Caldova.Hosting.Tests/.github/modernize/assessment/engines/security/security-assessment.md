# Security Assessment Report

**Generated:** 2026-11-11T05:28:31.543Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 1 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 1 |
| Total Rules Assessed | 59 |
| Rules Passed | 58 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 0 |
| optional | 0 |
| potential | 1 |

## CWE Findings (Code-Level Vulnerabilities)

### CWE-778: Insufficient Logging
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story Points:** 3
- **Files:** IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Controllers/AccountController.cs:30-35

The Login action processes password sign-in results, including failed attempts and lockouts, but its failure branches only return a view or add a generic model error and do not record an application audit event. The application-specific telemetry provider logs generic external-operation exceptions, not these authentication outcomes.
