# Post-migration hosting test plan

Source: the frozen 16-case bundle under
`tests/Caldova.Hosting.Tests/test-cases/`. No decision-table changes.

All tests live in `tests/Caldova.Hosting.Tests/HostingPostMigrationIT.cs`,
class `HostingPostMigrationIT`. Method names start with the TC ID, replacing
hyphens with underscores. C/E/U mean the exact fixture paths declared in the
frozen spec. All fixtures are copied to test output and loaded from disk.

| TC ID | Method suffix | Declared entry point | Trigger mechanism | Fixtures loaded | Non-mock dependencies | Config source | Cleanup |
|---|---|---|---|---|---|---|---|
| TC-WEB-001 | Health | GET /health | WebApplicationFactory HTTP client | C, E | None | C plus per-test key directory | Dispose host and temporary directory |
| TC-WEB-002 | MissingSql | Program.Main | WebApplicationFactory host start | C, E | None | C with missing/empty/whitespace SQL | Dispose failed host and directory |
| TC-WEB-003 | AppServiceHome | GET /health | HTTP client, then key-ring observation | C, E | None | C plus simulated HOME and instance ID | Dispose host and directory |
| TC-WEB-004 | MissingHome | Program.Main | WebApplicationFactory host start | C, E | None | C with missing HOME | Dispose failed host and directory |
| TC-WEB-005 | MissingProductionKeys | Program.Main | WebApplicationFactory host start | C, E | None | C without production key storage | Dispose failed host and directory |
| TC-WEB-006 | RelativeKeyPath | Program.Main | WebApplicationFactory host start | C, E | None | C with relative key directory | Dispose failed host and directory |
| TC-WEB-007 | HttpsAndUntrustedForwarding | GET /health | HTTP and HTTPS clients | C, E | None | C plus key directory | Dispose host and directory |
| TC-WEB-008 | HostFiltering | GET /health | HTTP client with rejected Host | C, E | None | C AllowedHosts | Dispose host and directory |
| TC-WEB-009 | StaticAssets | GET static assets | HTTP client, compare hash | C, E | None | C plus key directory | Dispose host and directory |
| TC-WEB-010 | AdminChallenge | GET /Admin/Orders | Anonymous HTTP client | C, E | None | C plus key directory | Dispose host and directory |
| TC-WEB-011 | LoginAcrossRestart | POST /Account/Login; GET /Orders | HTTP clients and antiforgery round trip across independent hosts | C, E, U | None | C, U, shared temporary key directory | Dispose both hosts and directory |
| TC-WEB-012 | InvalidLogin | POST /Account/Login | HTTP client and antiforgery round trip | C, E, U | None | C, U | Dispose host and directory |
| TC-WEB-013 | SecureSession | GET /ShoppingCart | HTTP client | C, E | None | C, empty isolated EF provider | Dispose host and directory |
| TC-WEB-014 | SignalRNegotiation | POST /signalr/negotiate | HTTP POST valid and invalid versions | C, E | None | C plus key directory | Dispose host and directory |
| TC-WEB-015 | ProductionFailure | GET /api/products | HTTP client; interceptor throws before SQL IO | C, E | None | C plus key directory | Dispose host and directory |
| TC-WEB-016 | DevelopmentHttp | GET /ShoppingCart | HTTP client | C, E | None | C, Development, empty EF provider | Dispose host and directory |

No whole-business-flow tests, live dependency tests, cloud credentials,
test-only HTTP endpoints, or separate infrastructure sanity tests are added.

## Trigger-line audit

| TC | Trigger pseudocode | Entry point type | Forbidden pattern? |
|---|---|---|---|
| 001 | client.GetAsync("/health") | HTTP | No |
| 002 | factory.CreateClient() | Process | No |
| 003 | client.GetAsync("/health") | HTTP | No |
| 004 | factory.CreateClient() | Process | No |
| 005 | factory.CreateClient() | Process | No |
| 006 | factory.CreateClient() | Process | No |
| 007 | client.SendAsync(GET health with headers) | HTTP | No |
| 008 | client.SendAsync(GET health with Host) | HTTP | No |
| 009 | client.GetAsync(assetPath) | HTTP | No |
| 010 | client.GetAsync("/Admin/Orders") | HTTP | No |
| 011 | client.PostAsync("/Account/Login", form); restartedClient.GetAsync("/Orders") | HTTP | No |
| 012 | client.PostAsync("/Account/Login", form) | HTTP | No |
| 013 | client.GetAsync("/ShoppingCart") | HTTP | No |
| 014 | client.PostAsync("/signalr/negotiate?negotiateVersion=1", empty) | HTTP | No |
| 015 | client.GetAsync("/api/products") | HTTP | No |
| 016 | client.GetAsync("/ShoppingCart") | HTTP | No |
