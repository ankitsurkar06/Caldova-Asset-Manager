# Infra Decision Table

## Metadata

| Field | Value |
|---|---|
| Project | Caldova-Asset-Manager |
| Module | Caldova-Asset-Manager.Website |
| Migration Scope | Native Windows App Service hosting, one instance |
| Integration Test Environment | mock |
| Created At | 2026-10-27 |
| `infra/` Snapshot | infra-missing |
| Status | baseline (frozen) |

Confirmed by the user before saving: every external dependency uses mock mode
and test authentication is n/a. No cloud endpoint or real credential is used.

| Dependency | Infra Match | Decision | Auth Method | Reason |
|---|---|---|---|---|
| Existing SQL, `DefaultConnectionString` | No | mock | n/a | infra-missing; replace the EF provider with isolated in-memory data, or intercept SQL opening before any network access for the error case. |
| SMTP, `Email:SmtpHost` | No | mock | n/a | infra-missing; disabled for hosting tests, with no email-delivery trigger in scope. |
| Facebook, `Authentication:Facebook` | No | mock | n/a | infra-missing; explicitly disabled in test configuration. |
| Google, `Authentication:Google` | No | mock | n/a | infra-missing; explicitly disabled in test configuration. |
| Microsoft, `Authentication:Microsoft` | No | mock | n/a | infra-missing; explicitly disabled in test configuration. |
| Twitter, `Authentication:Twitter` | No | mock | n/a | infra-missing; explicitly disabled in test configuration. |
| Recommendations, `MachineLearning:ScoringEndpoint` | No | mock | n/a | infra-missing; explicitly disabled in test configuration. |
| Windows App Service IIS and HTTPS termination | No | mock | n/a | infra-missing; simulate requests with TestServer, not an actual App Service proxy. |
| App Service HOME key storage | No | mock | n/a | infra-missing; use a unique temporary directory to simulate durable storage across two independent hosts. |

Local filesystem operations are real local operations, not validation of Azure
storage durability or permissions. No test establishes Azure connectivity,
SQL schema compatibility, SMTP delivery, provider callbacks or SignalR scale-out.
