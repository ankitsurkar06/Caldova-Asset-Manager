# App Service hosting verification

**Status: success for the approved local/mock readiness scope.**
Production deployment and live dependency certification are not included.
Executed 2026-10-27 against the current .NET 10 working tree.

## Scope and implementation

- Validated the SQL setting at startup, without opening SQL at startup or
  creating/migrating/seeding the database.
- Preserved explicit key-path precedence; added a durable App Service HOME
  default and explicit errors for missing production storage/relative paths.
- Set identity, session and antiforgery cookies to Always Secure outside
  Development; preserved Development HTTP.
- Preserved native Windows IIS integration and rejected arbitrary forwarded
  scheme/host assertions from an untrusted caller.
- Excluded historical `Models/project.json` metadata from publishing, alongside
  existing legacy configuration exclusions.
- Preserved the existing runtime, SQL mapping, routes and optional integrations.
  No new Azure services, shared session, SignalR backplane, cloud writes or CVE
  work were introduced.

The [frozen dependency table](../../../../../tests/Caldova.Hosting.Tests/test-cases/infra-decision-table.md)
was loaded unchanged. The [TC-to-test plan](post-migration-plan.md) contains one
row per case and an outside-in trigger audit. The frozen bundle matches its
latest baseline-only Git commit.

## Runtime Execution Evidence

Commands run from the repository root:

```powershell
# Initial new test dependencies were restored with auditing explicitly excluded.
dotnet test .\tests\Caldova.Hosting.Tests\Caldova.Hosting.Tests.csproj `
    -c Release --logger 'trx;LogFileName=hosting.trx' `
    --results-directory .\artifacts\test-results -p:NuGetAudit=false

# Final execution:
dotnet test .\tests\Caldova.Hosting.Tests\Caldova.Hosting.Tests.csproj `
    -c Release --no-restore --logger 'trx;LogFileName=hosting.trx' `
    --results-directory .\artifacts\test-results -p:NuGetAudit=false

dotnet publish .\IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj `
    -c Release --no-restore -o .\artifacts\webapp -p:NuGetAudit=false
```

Final runner result:

```text
Passed! - Failed: 0, Passed: 16, Skipped: 0, Total: 16
```

TRX counters: total=16, executed=16, passed=16, failed=0, error=0,
notExecuted=0. All **16 executed PostMigrationIT methods equal the 16 planned
TCs**. No integration method was skipped. Release compilation/publishing passed.
This is actual application execution through WebApplicationFactory, not merely
compilation or a helper-only unit test.

Machine-readable results:
[hosting.trx](../../../../../artifacts/test-results/hosting.trx)
(generated/ignored build output).

### Per-case dependency evidence

Every case runs its own full web host through WebApplicationFactory. Each
successful host uses a new isolated EF InMemory database unless explicitly
noted. No real/testcontainer dependency was selected or contacted.

| TC | Result | Dependencies actually used and assertions |
|---|---|---|
| TC-WEB-001 | Passed | Mock host; exact health JSON; zero SQL opens and no session cookie. |
| TC-WEB-002 | Passed | Three failed host starts for absent/empty/whitespace SQL; explicit diagnostic; zero SQL opens. |
| TC-WEB-003 | Passed | Simulated App Service HOME on a unique local directory; actual key XML observed under the dedicated HOME path, none under content root; zero SQL opens. |
| TC-WEB-004 | Passed | Simulated App Service without HOME; explicit startup failure and zero SQL opens. |
| TC-WEB-005 | Passed | Non-Azure Production without configured key storage; explicit failure and zero SQL opens. |
| TC-WEB-006 | Passed | Relative key path; explicit failure and no relative directory created. |
| TC-WEB-007 | Passed | TestServer HTTP/HTTPS; 307 target and HSTS; caller forwarding headers did not bypass redirect; zero SQL opens. |
| TC-WEB-008 | Passed | TestServer host filtering; unapproved Host returned 400, not successful health; zero SQL opens. |
| TC-WEB-009 | Passed | Published-source static file serving; exact CSS SHA256 and missing-file 404; zero SQL opens. |
| TC-WEB-010 | Passed | Real Identity challenge middleware; 302 HTTPS login target and decoded ReturnUrl; zero SQL opens. |
| TC-WEB-011 | Passed | Seeded EF test user and real Identity/antiforgery round trip; Always Secure policy and Secure/HttpOnly cookie; independent restarted host accepted the cookie with the same filesystem ring; same key IDs and user identity observed; explicit key path overrode simulated HOME; zero SQL opens. |
| TC-WEB-012 | Passed | Seeded EF test user; incorrect password denied with exact message, no identity cookie, subsequent Orders challenge, user preserved; zero SQL opens. |
| TC-WEB-013 | Passed | Empty EF test provider and local session; cart HTML, Always Secure policy, Secure/HttpOnly session cookie; no cart/order/user rows; zero SQL opens. |
| TC-WEB-014 | Passed | Real Core SignalR negotiation within TestServer; version/token/transport assertions; invalid version produced the exact protocol error without a connection token; zero SQL opens. |
| TC-WEB-015 | Passed | Production SQL provider plus DbConnectionInterceptor throwing before opening the connection; exactly one intercepted attempt; sanitized 500 problem title/detail/status and no sentinel/connection setting in response. |
| TC-WEB-016 | Passed | Development TestServer, empty EF test provider and ephemeral test key provider; HTTP cart response, SameAsRequest policy, HttpOnly/non-Secure session cookie, no business rows and zero SQL opens. |

Test configuration and payload expectations were loaded from the frozen
fixtures. User credentials are fabricated test fixtures, not a real account.
SMTP, Facebook, Google, Microsoft, Twitter and recommendations were not invoked;
social providers/recommendations were explicitly disabled. No fallback to real
SQL was possible for the error case: the interceptor throws before network IO.
The successful cases replace the SQL provider with EF InMemory rather than
contacting the inert fixture connection string.

## Baseline correction record

The initial execution passed 14 cases and failed two incorrect spec expectations.
The original source already called `Results.Problem` with a detail argument,
leaving its standard title. The original framework SignalR implementation
already returned HTTP 200 with a protocol error for a non-integer version.
The published artifact independently confirmed the exact negotiation error.
The baseline was explicitly unfrozen, corrected, revalidated and re-frozen
before rerunning. The case count and dependency decisions did not change.
No production behavior was changed to accommodate those errors; exact JSON
field assertions now preserve those existing contracts.

## Published artifact evidence

- Generated IIS configuration uses `AspNetCoreModuleV2`, `dotnet`,
  `.\Caldova-Asset-Manager.dll` and `inprocess`.
- Both shared-framework minimums in runtimeconfig are 10.0.13.
- Content, Images, Scripts, fonts and favicon exist. Bootstrap CSS SHA256
  matches the frozen fixture.
- Historical XML transforms, Web.Legacy.config, packages.config and
  Models/project.json are absent. The packaged appsettings contains no
  connection-string entries.
- ZIP contains **220 entries**, **11,663,099 bytes**, and the application files
  directly at its root.

Artifact:
[Caldova-Asset-Manager-AppService.zip](../../../../../artifacts/Caldova-Asset-Manager-AppService.zip)

SHA256:
`7B098BC9CB2F7EC495F90C0EDD7A6E6E0322FDAC9AE5A1DB161993725D8C4E2E`

The initial packaging assertion assumed the generated system.webServer was
wrapped in a location node. The SDK's final output placed it directly under
configuration, which is also valid. Validation was corrected to accept either
valid structure while still asserting all required hosting values. No IIS
configuration workaround was applied to the application.

### Published-process checks

Started the published DLL twice using `dotnet <published DLL> --urls
http://127.0.0.1:<ephemeral port> --contentRoot <publish directory>`:

1. Production, simulated App Service HOME, no explicit key path: startup
   succeeded; `/health` returned 307 to the HTTPS origin; untrusted forwarding
   headers did not bypass or alter the redirect.
2. Development: `/health` returned exact liveness JSON, the static CSS hash
   matched, and Core SignalR negotiation returned a version-1 token.

Both helpers bound only to loopback. Processes were stopped by their specific
PIDs, and temporary logs/key directories were removed. The source snapshot
remains available as the baseline artifact. These checks are not a real HTTPS
handshake, an IIS-hosted test or a platform durability test.

## Remaining mandatory live gates

The [deployment runbook](../../../../../docs/AZURE-APP-SERVICE-DEPLOYMENT.md)
contains the full checklist and rollback instructions. Before production:

- Confirm actual Windows App Service runtime availability and one-instance
  configuration.
- Validate public HTTPS/IIS termination, callbacks, host filtering and cookies.
- Validate durable key storage/permissions and identity-cookie continuity after
  an actual platform restart or slot operation.
- Establish real SQL reachability, TLS, identity permissions and existing schema
  compatibility; exercise login, browsing, cart, checkout, orders and admin
  against approved test data.
- Verify real SignalR transport and every enabled remote integration.
- Accept process-local session/cart loss across recycle, and approve release
  and rollback. Multi-instance operation remains out of scope.

No Azure provisioning/deployment, live SQL/SMTP/provider/recommendation testing,
runtime upgrade or CVE scan/remediation was performed. These omissions are
explicit scope boundaries, not claims of successful production certification.
