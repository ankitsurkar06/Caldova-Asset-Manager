# Hosting baseline

Captured 2026-10-27 before application edits. The website snapshot contains 318
files in the current Copilot session's `files/app-service-baseline-source`
directory. Build output and user-specific project files were excluded.

Command:

```powershell
dotnet build .\IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj -c Release --no-restore
```

Result: passed, zero warnings and zero errors.

Only Caldova-Asset-Manager.Website is in scope. The SQL database project and workshop
deployment project are out of scope; neither is modified or built for hosting
readiness. No existing test projects or executable integration tests were found.
New tests use the conventional .NET `tests/` layout, xUnit, and
WebApplicationFactory; the frozen spec establishes these conventions.

Frozen bundle:
[test-cases](../../../../../tests/Caldova.Hosting.Tests/test-cases/test-cases.md),
[dependency decisions](../../../../../tests/Caldova.Hosting.Tests/test-cases/infra-decision-table.md).
Fixtures: `configuration/hosting.json`, `expectations/hosting.json`, and
`seed-data/user.json` under that bundle's `testdata/`.

The 16 cases cover host startup and the HTTP boundaries listed in the spec.
Counts: 7 happy-path, 2 boundary, 1 special-input, 6 failure; zero cases are
covered by pre-existing tests. Approved behavior changes are explicitly recorded
alongside the preserved baseline contracts.

User confirmed mock mode and every dependency decision before writing the
bundle. SQL uses an isolated EF test provider or a pre-network connection
interceptor. SMTP, social providers and recommendations are not invoked.
Azure/IIS and HOME storage are local simulations. No real credentials or
resource identifiers are needed.

Required fields, fixture paths and unique IDs were checked before freezing.
Freeze integrity is anchored by the baseline-only Git commit and checked again
before post-migration execution.

## Re-freeze 1

The first execution passed 14 cases and identified two specification defects.
TC-WEB-014 expected a transport-level 400 for an existing SignalR protocol error;
direct execution of the published artifact confirmed HTTP 200 with
`The client requested a non-integer protocol version.` in the JSON error field.
TC-WEB-015 confused the `Results.Problem` detail argument with its title.
The existing production handler and SignalR implementation were not changed.
Both expectations were corrected and strengthened to assert their exact JSON
fields. The required-field, fixture and unique-ID freeze gate was repeated.
The resulting baseline-only commit supersedes the original expectations.
