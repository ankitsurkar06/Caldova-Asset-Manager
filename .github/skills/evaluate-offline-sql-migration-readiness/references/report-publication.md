# Self-Contained Assessment Report

The package metadata sets the host report-link title to `Azure SQL readiness`.
This display title does not change the skill ID, report paths or native report
contents.

For HTML including default or performance-based SKU, the skill calls the existing
publisher after every per-instance helper succeeds. Follow the short
[batch commands](command-execution.md), then verify the host `index.html`.
Use the block below for Markdown or deferred single-instance publication only
after all suitability, SKU and combined results are verified.

This reference describes single-instance publication. For multiple discovered
instances, follow [automatic discovery](automatic-discovery.md): require every
target to complete and use the packaged batch publisher to embed all complete
native HTML documents in one self-contained entry point. Original native files
remain unchanged; the single-instance entry point remains byte-identical.

Use the supplied `assessment-output-dir` and `report-format` (`html` or
`markdown`). Do not append a skill name, timestamp or another directory, infer
the format, request another destination or change the original native artifacts.

## Eligibility

Publish only after the requested assessment has completed with verified usable
results. Missing inputs, failed prerequisites, invalid results, running work and
failed or timed-out execution must not create `index.html` or `index.md`.
Require verified SKU completion for all three targets too. A failed sizing workflow must not be published as a
completed combined request.
Honor a caller's instruction to retain native files without publishing.

For HTML, publish the verified native combined assessment HTML unchanged, preserving its
full findings and interactive presentation. For Markdown, keep the original
readiness template, generation time, reuse notes and SKU guidance or the complete
requested SKU recommendation template. Do not invent results or readiness.
Missing or invalid assessment data is not completion.

The report must contain the full rendered results, not a link-only placeholder.
Native file paths may remain as provenance text, but do not make local-file links
or depend on sibling files. HTML embeds its presentation; Markdown embeds all
results. Preserve the original native HTML and JSON in the invocation directory.
Keep supporting SKU Markdown, JSON and HTML in that directory when generated.
Only the format-matching entry point is copied into the final report output;
companion artifacts are not published. The combined HTML contains suitability and
SKU recommendations; do not substitute the separate readiness-only HTML.
Markdown publication includes the complete requested SKU template in `index.md`.
Never include credentials, connection strings, protected configuration, signed
operation URLs or raw native errors.

## Write the requested entry point

Bind `$ASSESSMENT_OUTPUT_DIR` and `$REPORT_FORMAT` from the supplied output
context. For deferred HTML, set `$NATIVE_HTML_PATH` to the `HtmlPath` in the
per-instance helper's validated result for the requested instance.
`Get-OfflineAssessmentResult` runs inside the packaged entry point, not copied
into another terminal invocation. A fresh result may report
the server's canonical name rather than its requested IP address or alias; use
only the unique result validated against the single-connection run and pre-run
file snapshot. Never choose an arbitrary
HTML file or scan another invocation. For a new run, verify the matching HTML was
written after the recorded assessment start time before publication.

For Markdown, set `$REPORT_TITLE` to the readiness business title and
`$REPORT_CONTENT` to the complete original rendered result and notes.
Only after the eligibility checks, execute this block in the local process.
HTML must exist, be nonempty and be self-contained; missing native HTML or
external assets are explicit publication failures, not a summary-report fallback.
Markdown keeps its existing tables or fenced-text presentation unchanged.

```powershell
$format = ([string]$REPORT_FORMAT).Trim().ToLowerInvariant()
if ($format -notin @('html', 'markdown')) {
  throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: A supplied report-format of html or markdown is required."
}
if ([string]::IsNullOrWhiteSpace([string]$ASSESSMENT_OUTPUT_DIR) -or
    -not [IO.Path]::IsPathFullyQualified([string]$ASSESSMENT_OUTPUT_DIR) -or
    -not [IO.Directory]::Exists([string]$ASSESSMENT_OUTPUT_DIR)) {
  throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: An existing absolute assessment-output-dir is required."
}
$directory = [IO.Path]::GetFullPath([string]$ASSESSMENT_OUTPUT_DIR)
if ($format -eq 'html') {
  if ([string]::IsNullOrWhiteSpace([string]$NATIVE_HTML_PATH) -or
      -not [IO.Path]::IsPathFullyQualified([string]$NATIVE_HTML_PATH) -or
      -not [IO.File]::Exists([string]$NATIVE_HTML_PATH)) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_DATA_UNAVAILABLE: Matching native assessment HTML is required for HTML publication. Retain the native artifacts; use Markdown or investigate native HTML generation outside assessment."
  }
  $native = [IO.FileInfo]::new([string]$NATIVE_HTML_PATH)
  if ($native.Extension -ine '.html' -or $native.Length -eq 0 -or
      -not [string]::Equals($native.DirectoryName, $directory, [StringComparison]::OrdinalIgnoreCase) -or
      ($native.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: A nonempty regular native HTML file from the validated invocation is required."
  }
  $nativeHtml = [IO.File]::ReadAllText($native.FullName)
  if ($nativeHtml -notmatch '(?i)<html(?:\s|>)') {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: The matching native report is not a complete HTML document."
  }
  $assetPatterns = @(
    '(?is)<(?:script|link|img|image|iframe|source|audio|video|embed|object|use)\b[^>]*\b(?:src|href|poster|data)\s*=\s*(?:"(?<asset>[^"]*)"|''(?<asset>[^'']*)''|(?<asset>[^\s>]+))',
    '(?is)url\s*\(\s*(?:"(?<asset>[^"]*)"|''(?<asset>[^'']*)''|(?<asset>[^\s)]+))\s*\)'
  )
  if ($nativeHtml -match '(?i)@import\b') {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_NOT_SELF_CONTAINED: Native HTML imports external styles. Retain the native artifacts; its presentation must be embedded before publication."
  }
  foreach ($pattern in $assetPatterns) {
    foreach ($assetMatch in [regex]::Matches($nativeHtml, $pattern)) {
      $asset = $assetMatch.Groups['asset'].Value.Trim()
      if ($asset -and -not $asset.StartsWith('data:', [StringComparison]::OrdinalIgnoreCase) -and
          -not $asset.StartsWith('#', [StringComparison]::Ordinal)) {
        throw "[evaluate-offline-sql-migration-readiness] REPORT_NOT_SELF_CONTAINED: Native HTML requires a companion or remote asset. Retain the native artifacts; its presentation must be embedded before publication."
      }
    }
  }
  $entryPoint = Join-Path $directory 'index.html'
} else {
  if ([string]::IsNullOrWhiteSpace([string]$REPORT_TITLE) -or
      [string]::IsNullOrWhiteSpace([string]$REPORT_CONTENT)) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Complete verified readiness content and its business title are required."
  }
  $report = '# ' + [string]$REPORT_TITLE + "`n`n" + [string]$REPORT_CONTENT + "`n"
  $entryPoint = Join-Path $directory 'index.md'
}
$temporaryPath = Join-Path $directory ('.sql-report-' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
  if ($format -eq 'html') {
    [IO.File]::Copy($native.FullName, $temporaryPath, $false)
  } else {
    [IO.File]::WriteAllText($temporaryPath, $report, [Text.UTF8Encoding]::new($false))
  }
  [IO.File]::Move($temporaryPath, $entryPoint, $true)
} catch [IO.IOException] {
  throw "[evaluate-offline-sql-migration-readiness] REPORT_PUBLICATION_FAILED: The requested report could not be written. Prepare a writable output destination outside assessment."
} catch [UnauthorizedAccessException] {
  throw "[evaluate-offline-sql-migration-readiness] REPORT_PUBLICATION_FAILED: The requested report could not be written. Prepare a writable output destination outside assessment."
} finally {
  if ([IO.File]::Exists($temporaryPath)) { [IO.File]::Delete($temporaryPath) }
}
```

Verify that the requested entry point exists and is nonempty. For HTML, compare
its SHA-256 with the selected native HTML: publication must preserve the bytes
and the original file. For Markdown, verify the complete rendered result.
Do not claim publication when writing or verification failed.
Keep report content in the file; the final outcome briefly confirms the result
and publication rather than repeating the full report body.
