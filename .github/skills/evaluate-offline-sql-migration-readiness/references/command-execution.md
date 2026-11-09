# Offline readiness execution

The skill checks inputs and prerequisites, selects the native tool, discovers
targets, invokes each target and publishes the completed batch. There is no
packaged all-in-one pipeline. Execute short local commands directly; do not
extract Markdown at runtime, generate a workflow wrapper or pipe multiline code
to `pwsh -Command -`: the observed stdin wrapper sometimes exited zero without
executing its body.

## Protected per-instance execution

Only the credential-bearing native lifecycle remains packaged. Run the installed
helper with `pwsh -NoProfile -NonInteractive -File`, using literal non-secret
selectors:

```powershell
pwsh -NoProfile -NonInteractive -File "C:\InstalledSkill\scripts\Invoke-SqlReadinessAssessment.ps1" -InstanceName "localhost" -AssessmentOutputDirectory "C:\InvocationOutput" -SqlAssessmentPath "C:\PreparedTool\SqlAssessment.exe"
```

`InstanceName`, `AssessmentOutputDirectory` and `SqlAssessmentPath` are required.
`AuthMode`, `ConfigFilePath`, `SqlUserName`, `KeyVaultName`, `SecretName`,
`CredentialTarget`, `TrustServerCertificate`, `PerformanceDataPath`, `ReportFormat`
and `Fresh` retain their [input validation](input-validation.md) meanings.
`DeadlineUtc` passes the skill's shared seven-minute UTC deadline; never restart
the budget per target. Native calls are bounded and sanitize stdout/stderr.

The helper does not discover targets, download tools, check CLI versions or
publish. It returns one non-secret JSON result: canonical `InstanceName`,
`OutputDirectory`, `JsonPath`, combined `HtmlPath`, `GeneratedAtUtc`, `Reused`,
`SizingCriterion`, three `SkuJsonPaths` and `SuitabilityHtmlPath`.

The inseparable execution boundary is justified by observed failures:

- Connection reconstruction lost `.psbase.ConnectionString` semantics.
- Credentials and private configs must remain alive through all configuration
  sizing calls, then be removed in `finally`, including failures.
- Native exit zero did not establish usable output. Pre-run snapshots, completed
  JSON, instance/database/target metadata, fresh SKU artifacts and combined HTML
  are checked before accepting success. Stale or missing output never falls back
  to a success-shaped summary.

General requests reuse validated suitability JSON for the exact instance.
Unreadable JSON or orphaned HTML is not permission to refresh. Explicit fresh
intent bypasses reuse. A newly assessed canonical name may differ from the
requested alias only when single-connection provenance and unique fresh output
establish the binding. Sizing always uses a new isolated result set.

## Skill-led batch commands

Validate request-level inputs first, including explicit auth selectors and the
conflict between an explicit native build and forced refresh. Run the
[prerequisite commands](assessment-prerequisites.md), then
[prepare and capability-check the native tool](native-package.md).
Set `$skillPath` to the installed skill directory and `$DeadlineUtc` once to
`[DateTime]::UtcNow.AddMinutes(7)`. Keep the selected package's non-secret
provenance separately. `WorkspacePath` is the supplied existing application
directory; `AssessmentOutputDirectory` is the exact host destination.

The following ordinary local commands demonstrate target iteration and the
complete-batch gate. Do not turn them into another packaged pipeline. The resolver
returns only endpoint selectors and private config paths, never credentials.
Run resolution and cleanup in the same tool session using `try/finally`.

<!-- batch-commands -->
```powershell
$resolution = $null
$results = [Collections.Generic.List[object]]::new()
$failures = [Collections.Generic.List[string]]::new()
try {
    if ($InstanceName) {
        $targets = @([pscustomobject]@{ instanceName = $InstanceName; configFilePath = $ConfigFilePath })
    } else {
        $discovery = @{ WorkspacePath = $WorkspacePath }
        if ($ConfigFilePath) { $discovery.ConfigurationFile = $ConfigFilePath }
        elseif ($AuthMode) { $discovery.EndpointOnly = $true }
        $resolution = & (Join-Path $skillPath 'scripts\Resolve-SqlAssessmentTargets.ps1') @discovery | ConvertFrom-Json
        $targets = @($resolution.targets)
    }
    $ordinal = 0
    foreach ($target in $targets) {
        $ordinal++
        $invoke = @{
            InstanceName = $target.instanceName
            AssessmentOutputDirectory = $(if ($targets.Count -eq 1) { $AssessmentOutputDirectory } else { Join-Path $AssessmentOutputDirectory ('instances\{0:d4}' -f $ordinal) })
            SqlAssessmentPath = $SqlAssessmentPath
            DeadlineUtc = $DeadlineUtc
            Fresh = [bool]$Fresh
        }
        foreach ($name in @('AuthMode', 'ConfigFilePath', 'SqlUserName', 'KeyVaultName', 'SecretName', 'CredentialTarget', 'TrustServerCertificate', 'PerformanceDataPath', 'ReportFormat')) {
            $value = Get-Variable $name -ValueOnly -ErrorAction SilentlyContinue
            if ($null -ne $value -and [string]$value -ne '') { $invoke[$name] = $value }
        }
        if ($resolution -and (-not $AuthMode -or $AuthMode -eq 'ConfigFile')) {
            $invoke.AuthMode = 'ConfigFile'
            $invoke.ConfigFilePath = $target.configFilePath
        }
        try {
            $arguments = @('-NoProfile', '-NonInteractive', '-File', (Join-Path $skillPath 'scripts\Invoke-SqlReadinessAssessment.ps1'))
            foreach ($item in $invoke.GetEnumerator()) {
                if ($item.Key -eq 'Fresh') { if ($item.Value) { $arguments += '-Fresh' }; continue }
                $arguments += @(('-' + $item.Key), $(if ($item.Value -is [datetime]) { $item.Value.ToString('o') } else { [string]$item.Value }))
            }
            $remaining = [int]($DeadlineUtc - [DateTime]::UtcNow).TotalMilliseconds
            if ($remaining -le 0) { throw '[evaluate-offline-sql-migration-readiness] EXECUTION_TIMEOUT: Stage target-execution: the shared execution budget is exhausted.' }
            $run = Invoke-SqlTool (Get-Process -Id $PID).Path $arguments 'target-execution' ($remaining + 30000)
            if ($run.ExitCode -ne 0) { $failures.Add("Target ${ordinal}: $($run.Error.Trim())") }
            else { $results.Add(($run.Output | ConvertFrom-Json)) }
        } catch {
            $message = $_.Exception.Message
            if (-not $message.StartsWith('[evaluate-offline-sql-migration-readiness] ')) {
                $message = '[evaluate-offline-sql-migration-readiness] ASSESSMENT_FAILED: Stage target-execution: target did not complete; raw errors were withheld.'
            }
            $failures.Add("Target ${ordinal}: $message")
        }
    }
} finally {
    if ($resolution) {
        try {
            foreach ($target in $resolution.targets) { [IO.File]::Delete($target.configFilePath) }
            [IO.Directory]::Delete($resolution.privateDirectory)
        } catch { throw '[evaluate-offline-sql-migration-readiness] CLEANUP_FAILED: Stage cleanup: private discovery configuration could not be removed.' }
    }
}
if ($failures.Count) {
    throw "[evaluate-offline-sql-migration-readiness] ASSESSMENT_FAILED: $($failures -join ' ') Completed targets: $($results.Count)/$($targets.Count). No complete batch report was published."
}
$entryPoint = $null
if ($ReportFormat -eq 'html' -and -not $DeferPublication) {
    & (Join-Path $skillPath 'scripts\Publish-SqlAssessmentBatch.ps1') -OutputDirectory $AssessmentOutputDirectory -NativeHtmlPaths @($results | ForEach-Object HtmlPath)
    $entryPoint = Join-Path $AssessmentOutputDirectory 'index.html'
}
```

Always clean invocation-local package staging afterward, including failure, using
the native preparation reference. A target failure must not stop other selected
targets from being attempted within the shared deadline, nor permit partial
publication. Preserve each safe diagnostic, ordinal, stage and actual exit code.
The helper's captured nonzero exit is the failure gate. Do not print raw protected
input or native output.
The outer process wait allows thirty seconds after the execution deadline solely
for private-file cleanup; native work still shares the original deadline.

For HTML, the existing publisher copies one combined report byte-for-byte or
embeds every complete report in a self-contained batch. For Markdown or explicitly
deferred publication, render every instance with the full readiness and sizing
templates, then follow [publication](report-publication.md). No new assessment
is needed merely to publish.
