# Existing Local Tool Prerequisites

The skill checks these prerequisites before resolving credentials. Run short
commands in the local PowerShell tool session, not a stdin `-Command -` wrapper.
Keep raw responses local; only safe diagnostics may enter the model context.

- Windows and PowerShell 7.
- Azure CLI 2.75.0 or newer.
- An already installed `datamigration` extension.
- A native executable containing BOTH configuration-based sizing and combined
  saved-report generation, with the new SKU report metadata. The default workflow
  checks the Microsoft download-center package on every invocation, using a
  strong ETag and local archive SHA-256 to reuse unchanged downloads safely.
  Otherwise it downloads a fresh ZIP. `ForceSqlAssessmentRefresh` forces download.
  Each invocation extracts its own copy; it never replaces another application's
  shared executable. An explicit `SqlAssessmentPath` pins a prepared preview build
  and bypasses refresh; it cannot be combined with forced refresh.

Read `az version --output json` and parse the `azure-cli`
property locally. No quoted JMESPath expression is needed across `az.cmd`.
The installed extension list is also read as JSON. The native package is the
only automatic download. No CLI, extension or module is installed or upgraded,
and no sign-in occurs.

Keep prerequisite failures distinct:

1. Command failed: retain stage and observed exit code. An upgrade has not been
   determined necessary.
2. Successful command returned an unreadable version: verify the installation;
   do not infer an old version.
3. Successfully parsed version below the minimum: report installed and required
   versions and advise preparation outside assessment.
4. Extension-list command failed or returned invalid data: do not claim the
   extension is missing.
5. Valid list lacks `datamigration`, or an explicit native executable is absent:
   prepare the missing prerequisite outside assessment.
6. Native help does not confirm both sizing-criterion and combined-report options:
   the currently published package may not contain the preview APIs. Select a
   prepared build containing both features. Existing report
   metadata must also satisfy the combined generator's validation.
7. Freshness check/download, archive validation or cache metadata failure:
   report `PACKAGE_DOWNLOAD_FAILED` or `PACKAGE_INVALID`; never use a stale
   executable silently. Force refresh can replace unreadable cache metadata.

Raw stdout/stderr remain local. Preserve the script's safe diagnostic in the
final outcome. Never replace a query/launch/parsing failure with "Azure CLI
2.75.0 or later is required."

Azure sign-in is not required merely for Windows-integrated SQL authentication,
CredentialManager or protected SQL-login config. KeyVault and EntraDefault
require their existing noninteractive authorization. Credential resolution
occurs for a new suitability run or configuration-based sizing. Reused suitability
with performance-based sizing needs no SQL credentials.

Preparation references: [Azure CLI installation](https://aka.ms/installazurecli)
and the [assessment package](https://aka.ms/sqlassessmentpackage). Only the
assessment package is fetched automatically during skill execution.

## Local prerequisite commands

Set `$skillPath` to the installed skill directory. The process helper provides
bounded capture and safe failure classification; it does not orchestrate a workflow.

<!-- prerequisite-commands -->
```powershell
. (Join-Path $skillPath 'scripts\SqlAssessment.Process.ps1')
$az = Get-Command az -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $az) { throw '[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage prerequisites: Azure CLI must already be installed. No installation was attempted.' }
$response = Invoke-SqlTool $az.Source @('version', '--output', 'json') 'cli-version'
if ($response.ExitCode -ne 0) {
    throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage cli-version: Azure CLI version command failed; exit code $($response.ExitCode). Run az version outside assessment; an upgrade has not been determined necessary."
}
$version = $null
try { $valid = [version]::TryParse([string](($response.Output | ConvertFrom-Json -ErrorAction Stop).'azure-cli'), [ref]$version) } catch { $valid = $false }
if (-not $valid) { throw '[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage cli-version: unreadable version response; exit code 0; an upgrade has not been determined necessary.' }
if ($version -lt [version]'2.75.0') { throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage cli-version: installed Azure CLI version $version is below 2.75.0. Upgrade outside assessment." }
$response = Invoke-SqlTool $az.Source @('extension', 'list', '--output', 'json') 'extension-check'
if ($response.ExitCode -ne 0) { throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage extension-check: listing installed extensions failed; exit code $($response.ExitCode). Extension absence was not established." }
try {
    $extensions = @($response.Output | ConvertFrom-Json -ErrorAction Stop)
    if ($response.Output.Trim() -notmatch '^\[') { throw 'Expected an extension list.' }
} catch { throw '[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage extension-check: the installed extension list is unreadable. Verify Azure CLI outside assessment.' }
if (@($extensions | Where-Object name -eq 'datamigration').Count -ne 1) { throw '[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage extension-check: the datamigration extension must already be installed. No installation was attempted.' }
$response = $null
```

Select the executable using [native preparation](native-package.md). Then check
both capabilities with these non-secret commands. Native help writes to stderr
in supported builds, so inspect both captured streams.

<!-- capability-commands -->
```powershell
foreach ($probe in @(
    @{ Verb = 'GetSkuRecommendation'; Option = '--sizingCriterion' },
    @{ Verb = 'GenerateAssessmentReport'; Option = '--reportFolder' }
)) {
    $response = Invoke-SqlTool $SqlAssessmentPath @($probe.Verb, '--help') 'native-capabilities'
    if ($response.ExitCode -ne 0 -or -not ($response.Output + "`n" + $response.Error).Contains($probe.Option)) {
        throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: Stage native-capabilities: $($probe.Verb) help (exit code $($response.ExitCode)) did not confirm $($probe.Option). A prepared build containing both preview features is required."
    }
}
$response = $null
```
