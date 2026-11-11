# Task008 — configuration execution evidence

## Result

Implemented the independently feasible task008 configuration work on the actual
**net472 / MVC5** runtime. Local builds and focused offline checks pass.
This is **not** a .NET10 port. Overall dependency acceptance remains **pending**:
task001 is blocked and task004/007 are not implemented. The original confirmed
plan and all of tasks.json (including task008 pending status/dependencies) remain
byte-for-byte unchanged. This evidence records implementation without claiming
the original nine-task modernization is complete.

## Concrete changes

Workspace-relative:

1. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Recommendations\AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs`
   - Replaced embedded executable scoring URL with `MachineLearning.ScoringEndpoint`.
   - Endpoint and existing `MachineLearning.ModelName` use the existing
     `ExternalSecretConfiguration.GetSetting` resolver: CALDOVA_ environment
     overrides precede appSettings; no new configuration service/dependency.
   - Explicit required-value validation on request, not constructor. An endpoint
     must be absolute HTTPS with no credentials, query or fragment. Errors name
     settings, not their values, and escape neither into HTTP nor the old
     HttpRequestException catch-and-empty path.
   - Preserved quoted `Id`/`Item` parameters, order and Score path. Ordinary IDs
     produce the exact original URL. Data is URI-escaped, internal apostrophes
     doubled, and apostrophes explicitly encoded for older Framework URI modes.
   - Existing JSON response/empty-result/HTTP catch behavior unchanged.
2. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config`
   - Added public endpoint setting containing the original URL; added explanatory
     comment. Existing `sampletest` model, false recommendation feature flag and
     empty Key Vault account-key reference retained.
3. `docs\RECOMMENDATION-CONFIGURATION.md`
   - Documents endpoint/model/feature override names, file fallback, blank override
     semantics, required-value validation, request shape, lazy feature gating,
     existing Key Vault secret path, service availability and task009 limits.

New task008 evidence under `.github\modernize\plans\report-20261023210809`:

- `008-plan.md`, `008-progress.md`, `008-summary.md`
- `008-VerifyConfiguration.cs`, `008-RunConfiguration.ps1`
- `008-VerifyStructure.py`

No manual edits to csproj/packages, general ConfigurationHelpers, credential
resolver, HttpClientWrapper, controller, startup, identity/sign-in, database,
static assets, cache, prior-task evidence, README, unrelated docs or renames.
Generated ordinary bin/obj build artifacts and RecommendationConfigurationCheck
exe/config are validation output, not source implementation. The runner restores
its generated exe.config after variants; confirmed restored by XML comparison.

No branch query/switch, stash, reset, staging or commit was performed. The caller's
current branch `modernize/dotnet-20261023211948` was used as supplied. Whole-tree
dirty status cannot be attested: permitted version-control tooling did not expose
callable definitions. No direct Git commands or substitute Git operations used.
Edits were restricted to named files, preserving unrelated existing work.

## Exact successful commands

Run from `C:\Users\ankitsurkar\Caldova-Asset-Manager`:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Debug /nologo /verbosity:minimal
.github\modernize\plans\report-20261023210809\008-RunConfiguration.ps1
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Release /nologo /verbosity:minimal
.github\modernize\plans\report-20261023210809\008-RunConfiguration.ps1
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.sln /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
.github\modernize\plans\report-20261023210809\008-RunConfiguration.ps1
python .github\modernize\plans\report-20261023210809\008-VerifyStructure.py
python .github\modernize\plans\report-20261023210809\003-VerifyStructure.py
python .github\modernize\plans\report-20261023210809\005-VerifyStructure.py
python .github\modernize\plans\report-20261023210809\006-VerifyStructure.py
```

All final commands **exit0**. Runner compiles the focused C# check using:

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $arguments
```

Exact references/output/source/variant execution are in `008-RunConfiguration.ps1`.
No package restore or online dependency download was necessary.

| Validation | Result |
|---|---|
| Website Debug rebuild | Pass, no application build warning |
| Website Release rebuild | Pass; existing ManageController.cs(135,41) CS1998 |
| Existing renamed solution Debug build | Pass, no application build warning |
| Compiled offline check, Debug/Release/final solution output | **55 assertions each pass**: 43 primary + four file variants × 3 |
| Task008 local structure/consistency/completeness | Pass, five grouped checks |
| Prior SQL/secret/announcement structural regression checks | Pass; no prior task reimplemented |
| Existing unit-test projects | None found; no unit-test-project pass claimed |
| MCP build/test/CVE/consistency/completeness validators | Not callable; no tool-based pass claimed |
| Live scoring/Key Vault/SQL/cloud checks | Not performed; no availability claim |

The C# check invokes the actual compiled engine with recording IHttpClient and
telemetry fakes. It checks exact default/override URLs, repeated override reads,
fallback after removing overrides, Unicode and reserved-character encoding,
empty/null product compatibility, missing and empty file endpoint/model values,
blank host values, invalid endpoints (relative/non-HTTPS/credentials/query/fragment),
sanitized errors and no HTTP on invalid settings. Four generated exe.config
variants test actual file binding in separate processes, not a mocked resolver.
It exercises the real disabled MVC controller with null DB (returns EmptyResult),
lazy engine construction, disabled credential client without secret lookup,
feature environment switches, legitimate empty/nonempty JSON responses and the
unchanged legacy HTTP catch/telemetry behavior. No actual HTTP call is made.

Whitespace host override precedence is exercised. A literal empty environment
variable cannot be reliably retained by net472's Windows setter (empty removes
it); no claim of a literal-empty host runtime test is made. Structural inspection
confirms the unchanged resolver's null-coalescing (not blank-coalescing) fallback.

Structural checks fingerprint the unchanged confirmed plan, all task metadata,
project/packages, general helpers, credential resolver and controller; validate
net472, Compile/HintPath references, release/debug transforms, settings and docs.
They do not replace unavailable MCP validators or fix the known allOf schema bug.

## Internal failures and retries

1. Initial Debug build exit1: helper methods accidentally inserted between try
   and catch. Moved to class scope; all subsequent builds pass.
2. First compiled check exit1: older Framework URI mode leaves apostrophes
   unescaped. Added explicit apostrophe percent encoding; special-character
   request check now passes.
3. Missing-file test setup exit1: in-memory appSettings mutation encounters a
   read-only underlying configuration collection. Removed test reflection;
   runner now generates isolated exe.config file variants. All variants pass.
4. Release/solution command output exceeded initial waits. Used read_powershell
   on original sessions, not duplicate builds; both completed successfully.

## Tool limitations and outstanding work

Searches for appmod-run-task/version-control repeatedly returned tool names but
no callable definitions; no migration context/session or status could be obtained.
Build/test/CVE and consistency/completeness/preview discovery likewise exposed
names only. .NET dependency-guidance discovery did not yield callable guidance;
the targeted retry failed `net::ERR_NETWORK_IO_SUSPENDED`. No package changes
were needed. Tracking files were read locally instead of MCP preview.

No MCP execution/validation, CVE scan, cloud certification, integration suite,
baseline, Entra sign-in, provisioning, deployment or containerization is claimed.
The missing tools did not prevent independently feasible code and local checks.
No all-success final commit precondition is met; no commit performed.

No new recommendation service, Azure App Configuration or other configuration
service was invented. Legacy endpoint availability/compatible external replacement
contract remains an external prerequisite. Task009 must address HTTP transport
lifetime/error behavior; the legacy catch-and-empty is not silently certified as
correct. Upstream net10/Blob/Redis acceptance remains pending.
