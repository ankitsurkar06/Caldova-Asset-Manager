# Frozen App Service Hosting Baseline

Module: PartsUnlimitedWebsite. Frozen: 2026-10-27. Mode: mock.

## Scope and source anchoring

The public entry points are host startup (`Program.Main`), HTTP `GET /health`,
HTTP static asset requests, `GET /Admin/Orders`, `POST /Account/Login`,
`GET /Orders`, `GET /ShoppingCart`, `GET /api/products`, and
`POST /signalr/negotiate`. Their owners are `Program.cs` and the corresponding
controllers in the website. No existing tests were found.

The original Release build passed before any hosting changes. A source snapshot
was captured in the current Copilot session's `files/app-service-baseline-source`
directory, excluding build output and user-specific project files.

Existing observable contracts retained: health JSON, routes, static asset bytes,
authentication challenge paths, login, cart rendering, SignalR negotiation,
production problem responses, and Development HTTP behavior. Accepted deliberate
changes: missing SQL now fails at startup; production requires durable keys;
App Service derives its default key directory from HOME; relative key paths
fail explicitly; identity/session cookies are always Secure in production.
The tests below specify these approved target contracts, not a claim that the
unmodified app already met them.

All cases use:

- **C**: `testdata/configuration/hosting.json`
- **E**: `testdata/expectations/hosting.json`
- **U**: `testdata/seed-data/user.json`

These are real fixture paths relative to this file's directory. C contains an
inert test-only SQL setting. It must never be allowed to connect. U contains only
fabricated test credentials, not a deployable user. All random temporary paths
are generated per case. No test invokes a real remote integration.

## Cases

### TC-WEB-001
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: GET /health
- Trigger: HTTPS request using E.origin.
- Preconditions: Production host, C, explicit absolute temporary key path.
- Expected Response: 200; JSON exactly E.health.
- Resource Verification: No database connection opened; no session cookie.
- Negative Verification: No database reads or writes.
- Data References: C, E.

### TC-WEB-002
- Category: failure
- Entry Point Type: Process
- Entry Point: Program.Main
- Trigger: Start host with the SQL setting absent, empty, then whitespace.
- Preconditions: C with the connection setting replaced per trigger; absolute temporary key path.
- Expected Response: Startup throws InvalidOperationException containing E.sqlConfigurationError.
- Resource Verification: No SQL connection opened.
- Negative Verification: Host must not serve requests; no database reads or writes.
- Data References: C, E.

### TC-WEB-003
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: GET /health
- Trigger: HTTPS request, then create a data-protection payload to exercise the configured ring.
- Preconditions: C, simulated WEBSITE_INSTANCE_ID and HOME; no explicit key path.
- Expected Response: 200; JSON exactly E.health.
- Resource Verification: A key XML file is written below HOME/data/Caldova-Asset-Manager/keys; its root is key.
- Negative Verification: No key XML is written to the content root; no database access.
- Data References: C, E.

### TC-WEB-004
- Category: failure
- Entry Point Type: Process
- Entry Point: Program.Main
- Trigger: Start a Production App Service host without HOME or explicit key path.
- Preconditions: C; WEBSITE_INSTANCE_ID set; HOME empty.
- Expected Response: InvalidOperationException containing E.homeConfigurationError.
- Resource Verification: No SQL connection opened.
- Negative Verification: Host must not serve requests; no database writes.
- Data References: C, E.

### TC-WEB-005
- Category: failure
- Entry Point Type: Process
- Entry Point: Program.Main
- Trigger: Start Production outside App Service with no key path.
- Preconditions: C; WEBSITE_INSTANCE_ID and HOME empty.
- Expected Response: InvalidOperationException containing E.keyConfigurationError.
- Resource Verification: No SQL connection opened.
- Negative Verification: Host must not serve requests; no database writes.
- Data References: C, E.

### TC-WEB-006
- Category: boundary
- Entry Point Type: Process
- Entry Point: Program.Main
- Trigger: Start host with DataProtection:KeyPath equal to relative-directory.
- Preconditions: C; Production; explicit relative path.
- Expected Response: InvalidOperationException containing E.relativeKeyError.
- Resource Verification: No SQL connection opened.
- Negative Verification: Host must not serve requests; no relative key directory created.
- Data References: C, E.

### TC-WEB-007
- Category: special-input
- Entry Point Type: HTTP
- Entry Point: GET /health
- Trigger: HTTP request with X-Forwarded-Proto=https and X-Forwarded-Host=attacker.example.test; separately request HTTPS.
- Preconditions: C, Production, absolute temporary key path; untrusted caller, no IIS integration simulated.
- Expected Response: HTTP returns 307 to E.origin/health; HTTPS returns 200 with E.health and an HSTS max-age directive.
- Resource Verification: No SQL connection opened.
- Negative Verification: Caller-supplied forwarding headers must not bypass redirect or replace its host.
- Data References: C, E.

### TC-WEB-008
- Category: failure
- Entry Point Type: HTTP
- Entry Point: GET /health
- Trigger: HTTPS request with Host=attacker.example.test.
- Preconditions: C restricts AllowedHosts; Production with absolute temporary key path.
- Expected Response: 400.
- Resource Verification: No SQL connection opened.
- Negative Verification: No successful health response and no database writes.
- Data References: C, E.

### TC-WEB-009
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: GET /Content/bootstrap.min.css
- Trigger: HTTPS GET E.assetPath, then GET /Content/missing-hosting-test.css.
- Preconditions: C, Production, absolute temporary key path.
- Expected Response: Existing asset is 200 text/css with SHA256 E.assetSha256; missing asset is 404.
- Resource Verification: No SQL connection opened.
- Negative Verification: Missing asset must not return success or a replacement document.
- Data References: C, E.

### TC-WEB-010
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: GET /Admin/Orders
- Trigger: Anonymous HTTPS request without following redirects.
- Preconditions: C, Production, absolute temporary key path.
- Expected Response: 302 to E.origin/Account/Login with ReturnUrl decoding to /Admin/Orders.
- Resource Verification: No SQL connection opened.
- Negative Verification: No admin page content or database writes.
- Data References: C, E.

### TC-WEB-011
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: POST /Account/Login; GET /Orders
- Trigger: GET login page, extract antiforgery token/cookie, POST U credentials and returnUrl=/Orders; restart host using the same key directory and issue GET /Orders with the issued identity cookie.
- Preconditions: C, U seeded through the EF test provider; Production; same U identity and security stamp on both hosts.
- Expected Response: Login 302 to /Orders; identity cookie is Secure and HttpOnly; restarted host returns 200 containing E.ordersMarker.
- Resource Verification: Key XML exists; restarted host can read the original key ID; user ID/email retained and exactly one user remains.
- Negative Verification: No SQL network access; no schema creation or real account provisioning.
- Data References: C, E, U.

### TC-WEB-012
- Category: failure
- Entry Point Type: HTTP
- Entry Point: POST /Account/Login
- Trigger: GET login page for antiforgery, then POST U.email and U.wrongPassword.
- Preconditions: C, U seeded through EF test provider; Production; absolute temporary key path.
- Expected Response: 200 containing E.loginFailure; no identity cookie; subsequent GET /Orders returns 302.
- Resource Verification: Exactly one user remains with U.id and U.email.
- Negative Verification: No successful sign-in; no SQL network access.
- Data References: C, E, U.

### TC-WEB-013
- Category: boundary
- Entry Point Type: HTTP
- Entry Point: GET /ShoppingCart
- Trigger: Anonymous HTTPS request with no session cookie.
- Preconditions: C; empty EF test provider; Production; absolute temporary key path.
- Expected Response: 200 containing E.cartMarker; E.sessionCookie has Secure and HttpOnly.
- Resource Verification: Cart table remains empty.
- Negative Verification: No order or user rows created; no SQL network access.
- Data References: C, E.

### TC-WEB-014
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: POST /signalr/negotiate
- Trigger: HTTPS POST with negotiateVersion=1; then with negotiateVersion=invalid.
- Preconditions: C; Production; absolute temporary key path.
- Expected Response: Valid request 200; negotiateVersion=1, nonempty connectionId and connectionToken, WebSockets offered; invalid request 400.
- Resource Verification: No SQL connection opened.
- Negative Verification: Invalid negotiation must not allocate a usable connection.
- Data References: C, E.

### TC-WEB-015
- Category: failure
- Entry Point Type: HTTP
- Entry Point: GET /api/products
- Trigger: HTTPS request with SQL opening intercepted to throw a sentinel InvalidOperationException.
- Preconditions: C; Production; absolute temporary key path; interceptor blocks before network IO.
- Expected Response: 500 application/problem+json; status=500, title=E.problemTitle.
- Resource Verification: Exactly one attempted connection is intercepted.
- Negative Verification: Body contains neither the sentinel exception nor the SQL setting; no network IO or database writes.
- Data References: C, E.

### TC-WEB-016
- Category: happy-path
- Entry Point Type: HTTP
- Entry Point: GET /ShoppingCart
- Trigger: Anonymous HTTP request.
- Preconditions: C; Development; empty EF test provider; no App Service detection or configured key path.
- Expected Response: 200 containing E.cartMarker; E.sessionCookie has HttpOnly but not Secure.
- Resource Verification: Cart table remains empty.
- Negative Verification: No HTTPS redirect, no user/order rows, no SQL network access.
- Data References: C, E.

## Freeze checklist

All 16 IDs are unique. Each case declares its trigger, state, response, resource
verification, negative checks, and fixture references. Fixtures are externalized.
Cases cover successful, empty/boundary, untrusted-input and failure paths for
hosting-impacted boundaries. Whole-application business-flow coverage is not
claimed: real checkout, SQL compatibility, external providers and Azure/IIS
termination are release gates, not mocked hosting assertions.
