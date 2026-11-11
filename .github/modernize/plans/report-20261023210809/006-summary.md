# Task 006 — announcement compatibility execution evidence

## Result and acceptance status

Implemented feasible announcement-specific adjustments on **net472 / MVC5 / OWIN /
classic SignalR 2.2.2**, not an ASP.NET Core/.NET10 port. Local builds and focused
checks pass. **Overall task006 remains pending**, because task001 has not supplied
the modern runtime/SignalR port; task004 also remains blocked. Task006's description
records execution evidence while preserving status/dependencies/requirements.

No Azure SignalR, other messaging, scale-out, cloud resources, packages, runtime
upgrade, end-user sign-in migration, deployment, containerization, baseline or
integration suite was added. No cloud acceptance or live transport claim.

## Source changes (workspace-relative)

1. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\App_Start\Startup.Auth.cs`
   - The mapped hub and `GlobalHost.ConnectionManager` now share the same
     `SignalRDependencyResolver`, and therefore the same local message bus.
     Previously the map received a separate resolver from controller broadcasts.
   - Existing cookie/provider ordering and `/signalr` mapping retained.
2. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Areas\Admin\Controllers\StoreManagerController.cs`
   - Broadcast product URL explicitly clears ambient Admin area, producing public
     Store/Details URLs, including under an application virtual directory.
   - Corrected local variable spelling only otherwise. Save-before-broadcast,
     `Clients.All.announcement`, one ProductData payload, redirect and cache
     invalidation remain. No broadcast was added to edit/delete.
3. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\ViewModels\AlbumData.cs`
   - Existing ProductData has Newtonsoft `Title`/`Url` names and a per-property
     DefaultNamingStrategy. This protects exact casing even if a camel-case
     resolver is used. Existing library reused; no serializer-wide policy change.
4. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Scripts\NewStoreArrivals.js`
   - Dormant callback targets actual `a#new-arrival`, not absent NewArrivalsPanel.
     `.text(item.Title)` and `.attr("href", item.Url)` retained.
     Script was **not enabled**; initial hidden/no-model state remains.

Preserved unchanged: CLR `AnnouncementHub`, wire `[HubName("Announcement")]`,
generated `$.connection.Announcement`, event `announcement`, public receive access,
empty client-invocable hub method surface, inherited admin role, POST and
anti-forgery attributes. SignalR bundle, layout proxy references and callback
bundle inclusion remain commented. Server-rendered partial remains active.

No manual edits to project/solution names, dependencies, Web.config, routes,
bundles/layout/partial, prior 002/003/005 implementation/evidence, database/schema,
README or unrelated docs.

## Tracking/check files

All under `.github\modernize\plans\report-20261023210809`:

- `006-plan.md`, `006-progress.md`, `006-summary.md`.
- `006-VerifyAnnouncements.cs`: compiled offline contract harness.
- `006-RunAnnouncements.ps1`: repeatable compile/run command, outputs only to bin.
- `006-VerifyBrowser.js`: stub callback execution and browser reference checks.
- `006-VerifyStructure.py`: scope/task metadata/runtime/package consistency.
- `.metadata\tasks.json`: **only task006 description** changed.

Every other parsed metadata value is verified against its pre-edit fingerprint
after excluding task006 description:
`d2732ba99b65e2553d4a92c4d3f140362d0eb5dab74d36fae566f7cf9112f298`.
Task006 remains `pending`; no schema fix or claimed .NET10 completion.
Original confirmed `plan.md` unchanged.

Builds produce ordinary bin/obj outputs, plus AnnouncementCheck.exe/config.
These are generated validation artifacts, not additional source implementation.

## Exact successful validation commands

Run from `C:\Users\ankitsurkar\Caldova-Asset-Manager`:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Debug /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Release /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.sln /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
.github\modernize\plans\report-20261023210809\006-RunAnnouncements.ps1
node .github\modernize\plans\report-20261023210809\006-VerifyBrowser.js
python .github\modernize\plans\report-20261023210809\006-VerifyStructure.py
python .github\modernize\plans\report-20261023210809\003-VerifyStructure.py
python .github\modernize\plans\report-20261023210809\005-VerifyStructure.py
```

All final commands **exit0**.

| Check | Actual result |
|---|---|
| Website Debug rebuild | Success, no application build warning |
| Website Release rebuild | Success; existing `ManageController.cs(135,41)` CS1998 |
| Current renamed solution Debug build | Success, no application build warning |
| Compiled announcement harness | **42 assertions** passed against Debug, Release and final solution Debug outputs |
| Browser callback/reference harness | **13 assertions** passed; no browser/server launched |
| Task006 structure/metadata checks | Pass; four grouped checks, exact non-description metadata fingerprint preserved |
| Existing task003 structure checks | Pass; provider/config/build references/startup no-seed contracts retained |
| Existing task005 structure checks | Pass; credential/MI/SDK/disabled switches retained |
| Dependencies | All 81 package folders and HintPaths resolve; no package change |
| Existing unit-test projects | None found; no unit-test-project pass claimed |

The compiled harness invokes the **actual compiled private SignalR startup**
through a recording IAppBuilder (actual OWIN signature-conversion properties),
captures `/signalr` and HubConfiguration, and checks resolver/message-bus identity.
It generates the real legacy proxy in memory and invokes actual admin Create with
an in-memory ICaldovaAssetManagerContext and capturing IHubPipelineInvoker. Real MVC
routes/area registration/UrlHelper are used with stub HttpContext at `/` and
`/shop`; no DB connection or hosted HTTP transport. It checks mapped serializer
and camel-case override casing, roles (anonymous/non-admin denied, admin allowed
by AuthorizeCore), POST/anti-forgery attribute presence, save/broadcast ordering,
payload/event/all-clients signal, redirect/cache behavior, invalid model and
failed-save no-broadcast paths. This is not an end-to-end authorization or actual
anti-forgery request test.

Browser checks explicitly evaluate the dormant script in a VM with a tiny
DOM/SignalR stub, test exact selector/Title/Url/text sink, confirm partial IDs and
hidden state, and inspect project-owned references after removing comments.
They **do not** prove browser transport/rendering, network delivery or reconnect.

## Internal failures corrected and warnings

1. First standalone csc compile: exit1, missing System.Runtime facade required
   by existing Unity. Runner now explicitly references the installed net472 facade.
2. First harness startup: `MissingMethodException` for the incomplete builder's
   signature conversion support. Supplied actual AppBuilder properties.
3. Plain `[JsonProperty("Title")]`/`Url` failed the camel-case contract test.
   Added per-property `DefaultNamingStrategy`; subsequent serialization checks pass.
4. First MVC route invocation: stub HttpContext lacked GetService/Items.
   Implemented stub members; actual UrlHelper now passes both application paths.
5. Standalone csc still emits **CS1701** for existing Unity's System.Runtime4.0
   reference versus installed facade4.1.2. Website config copied for execution;
   compiled harness runs successfully. No project/package/binding change made to
   hide a test compiler warning. Release CS1998 was already documented by task005.

## Tool availability and scope limits

First planning/version-control discovery and focused retry returned names only,
not callable definitions. No appmod-run-task context/session ID, permitted git
status result, markdown preview or version-control action can be claimed.
Combined discovery for mandatory build/test/CVE/consistency/completeness,
preview and .NET dependency guidance failed `net::ERR_NETWORK_IO_SUSPENDED`.
Consequently those MCP checks **were not executed**, and local checks are not
represented as their passes. No CVE scan/remediation was run.

Permitted status inspection was attempted via discovery before code edits, but
unavailable; exact whole-tree dirty status cannot be attested. No direct git,
branch switch/stash/commit/reset was used. Caller-supplied branch
`modernize/dotnet-20261023211948` recorded without querying/changing it. Existing
unrelated dirty work was not deliberately changed or discarded. Overall migration
is blocked and does not meet the all-success condition for a final commit.

## Remaining work outside this task

- Task001 must supply the actual ASP.NET Core/.NET10 server/client SignalR port;
  then preserve CLR/wire/event names, choose compatible client routing and retain
  explicit Title/Url naming with that runtime's actual serializer. Newtonsoft
  attributes here are not a claim about a future System.Text.Json serializer.
- Revalidate local bus/server publisher integration after that port, preserving
  existing public subscription/admin-only publishing and disabled feature states.
- Hosted IIS/OWIN transport, live browser connections, cookies/anti-forgery request
  execution and delivery/reconnect were not exercised. No cloud test/claim.
- Task004 remains blocked. No baseline/integration suite, SQL/vault/identity/cloud
  operation, deployment or new messaging was performed to bypass dependencies.
