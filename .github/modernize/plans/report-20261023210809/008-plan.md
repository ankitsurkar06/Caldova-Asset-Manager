# Task008 — scoped execution plan

Date: 2026-10-23. Workspace: `C:\Users\ankitsurkar\Caldova-Asset-Manager`.
Branch supplied by caller: `modernize/dotnet-20261023211948`; no branch operations.
Session ID: unavailable (appmod-run-task search returns names, not callable definitions).

## Authority and scope

Read the confirmed `plan.md` and its `.metadata/tasks.json` in full. Execute only
the independently feasible portion of task008, as explicitly authorized. Actual
runtime is net472/MVC5, not the blocked .NET10 target. Do not repeat tasks001–007
or implement task009. Preserve existing dirty renames/docs and earlier code.
No rulebook, provisioning, deployment, containers, new configuration/recommendation
service, baseline or integration suite.

## Inspected behavior

- Recommendation engine embeds a scoring URL, reads MachineLearning.ModelName
  through file-only ConfigurationHelpers, and appends Id/Item quoted parameters.
- Existing ExternalSecretConfiguration.GetSetting already implements host
  CALDOVA_ overrides (dots to underscores, uppercase), including explicit empty
  override precedence. Reuse this resolver for public recommendation settings;
  no dependency or configuration-service addition.
- RecommendationsController gates execution with ShowRecommendations; Unity
  eagerly constructs the engine and HttpClientWrapper. Validation must remain
  lazy so disabled recommendations need neither endpoint nor model nor secrets.
- Key Vault credential resolution stays in HttpClientWrapper, unchanged.
- Existing response parsing and HTTP failure behavior belong to task009 and
  are not changed here.

## Exact intended changes

1. `Recommendations/AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs`:
   read MachineLearning.ScoringEndpoint and ModelName using the existing host
   resolver; explicitly validate nonblank required settings and an absolute HTTPS
   endpoint without credentials/query/fragment. Error messages identify setting
   names, never values. Preserve Score path and Id/Item quoted query contract;
   escape data values and double OData quote literals, not the endpoint.
   Perform validation before issuing a request, not in the constructor.
2. `Web.config`: add public MachineLearning.ScoringEndpoint default containing
   the old executable endpoint; retain model value, disabled flag and secret reference.
3. `docs/RECOMMENDATION-CONFIGURATION.md`: document settings/overrides, validation,
   existing Key Vault credential path and unverified legacy service availability.
4. Add only task008 evidence/compiled offline checks in this plan folder.
   Update only task008 description in tasks.json if recording execution;
   retain pending status/dependencies because upstream acceptance is blocked.

## Validation and limitations

- Attempt discovery of required build/test/CVE/consistency/completeness and
  dependency guidance tools; report lack of callable definitions honestly.
- Build website Debug/Release and existing renamed solution using installed
  MSBuild (local fallback when build tool cannot be called).
- Look for existing unit-test projects; run applicable tests if any exist.
- Compile a focused offline C# harness against the real website assembly with
  fake IHttpClient/telemetry. Check file defaults, endpoint/model overrides,
  empty overrides, malformed URLs, missing model, exact ordinary query shape,
  special-character encoding, no request on invalid config, lazy construction
  while disabled, legitimate empty/nonempty response behavior and unchanged
  controller/credential gating. No real HTTP/Key Vault/SQL calls.
- Run existing relevant structural checks, inspect changed-file scope, and
  document exact commands, failures/retries and results in 008-summary.md.
- Upstream tasks001/004/007 and .NET10 port remain blocked/not implemented.
  No live endpoint availability claim; task009 HTTP handling remains outstanding.
- Requested initial status inspection could not use appmod-version-control
  because discovery exposes names only; no direct Git commands or substitutes.
  No version-control operation, stash or commit will be performed here.
