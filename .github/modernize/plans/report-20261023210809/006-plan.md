# Task 006 execution plan — announcement compatibility

## Context and boundaries

- Workspace: `C:\Users\ankitsurkar\Caldova-Asset-Manager`.
- Caller-supplied branch: `modernize/dotnet-20261023211948`; no branch operations.
- Task: `006-transform-real-time`; no kbId supplied. Goal: announcement-specific
  compatibility adjustments, not the missing task-001 ASP.NET Core port.
- Session ID: unavailable. First tool discovery and a focused retry returned
  appmod-run-task/version-control names without callable definitions. No planning
  tool execution or permitted status inspection is claimed; no direct git.
- Read the confirmed `plan.md` and `.metadata/tasks.json` completely. Actual runtime
  remains net472/MVC5/OWIN/classic SignalR 2.2.2. Tasks 001/004 and overall
  modern-runtime acceptance remain blocked; preserve 002/003/005 and unrelated edits.
- No Azure SignalR, messaging/scale-out, provisioning/deployment/containerization,
  baseline, new integration suite, Entra user sign-in or schema changes.

## Analysis already observed (verify with focused executable checks)

- CLR `AnnouncementHub` explicitly exposes legacy wire name `Announcement`;
  browser uses `$.connection.Announcement`, client event `announcement`.
- Admin `Create` saves first then broadcasts to all; authorization is inherited
  from AdminController, and POST/anti-forgery validation must remain intact.
- Startup maps a custom resolver, while controller uses GlobalHost's different
  default resolver: align these so broadcasts use the mapped local message bus.
- ProductData Title/Url currently depend on default JSON naming. Pin those two
  names with existing Newtonsoft.Json attributes, without adding dependencies.
- Admin URL generation lacks an explicit empty Area; match public partial's
  Details/Store route with Area cleared to avoid ambient Admin routing.
- Disabled NewStoreArrivals.js targets absent NewArrivalsPanel; align selector
  with actual `new-arrival` anchor. Keep signalr bundle, script inclusion and
  commented layout reference disabled. Preserve hub's public receive access
  and empty method surface (no client-triggered publication).

## Ordered execution

1. Save/read this plan and initial `006-progress.md`; attempt markdown preview
   discovery. Permitted version-control inspection cannot run without a callable
   tool; record limitation without branch changes or direct git.
2. Analysis/dependencies: inspect routes, bundles, layout, partial, authorization,
   resolver and project references. Search .NET dependency guidance; reuse
   existing SignalR/Newtonsoft/OWIN, no package/project/config migration.
3. Code changes, only:
   - `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\App_Start\Startup.Auth.cs`: use the
     same resolver for GlobalHost and mapped SignalR.
   - `...\Areas\Admin\Controllers\StoreManagerController.cs`: clear Area
     in product announcement URL, correct local variable spelling.
   - `...\ViewModels\AlbumData.cs`: explicit JSON Title/Url contract.
   - `...\Scripts\NewStoreArrivals.js`: correct stale anchor reference;
     do not uncomment or register anything.
4. Add task-local offline checks (`006-VerifyAnnouncements.cs`,
   `006-VerifyBrowser.js`, structural checks as needed) and operator evidence.
   Exercise actual compiled payload serialization, resolver/message bus,
   generated proxy name/event, MVC route generation with ambient Admin and
   application path, inherited authorization, POST/anti-forgery and server hub
   method surface. Use fake/in-memory collaborators only; no database/network.
   Exercise disabled browser callback with a tiny Node DOM/SignalR stub, Title/Url,
   safe text updates and exact selector. Not an integration suite.
5. Verification: discover mandatory appmod build first, CVE, test, consistency and
   completeness tools. If callable, use required workflow; if names only, record
   each unavailable check honestly. Use installed MSBuild for focused Debug and
   Release rebuilds, existing tests if any (none found so far), offline harnesses
   and prior structural checks. Retry correctable local failures. No CVE success
   or test-project pass without execution. No live/browser-host acceptance claim.
6. Update this folder's progress/summary and only task006's description in
   `.metadata/tasks.json` with exact implemented scope/results/blockers; leave
   status pending because .NET10 prerequisite/acceptance is missing. Do not
   repair the known schema bug or rewrite prior task status.
7. No commit while overall migration is blocked. Preserve original confirmed
   plan, previous evidence, runtime/project/solution renames and unrelated files.

## Acceptance boundary

Feasible task006 code may pass local checks, but cannot establish compatibility
with ASP.NET Core/.NET10 before task001 supplies that runtime and client/server
port. Offline evidence does not certify an IIS session, live browser transport,
Azure messaging, identity login or SQL connectivity.
