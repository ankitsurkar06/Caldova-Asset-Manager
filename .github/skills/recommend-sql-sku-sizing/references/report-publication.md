# Supporting SKU Output

Save the complete [recommendation template](assessment-output.md) to
`sku-assessment.md` directly in the offline caller's supplied
`assessment-output-dir`. Retain the tool-generated JSON and HTML unchanged.
Preserve actual recommendation details, generation time and native report paths.
Do not append another directory or request another destination.

The offline caller owns the assessment entry point. Never generate custom HTML
or create `index.html` or `index.md` here. The caller appends the complete SKU
template to its Markdown report. These supporting artifacts stay in the invocation
directory: only the caller's format-matching entry point reaches the final report
output. The caller's native combined HTML includes suitability and SKU results.
Do not claim that a supporting file alone publishes a report.

Save only verified usable recommendations as completed output. Missing inputs,
failed prerequisites, unusable data and failed or timed-out execution must not
claim completion. New collection and collection-status output are unsupported.

Bind `$ASSESSMENT_OUTPUT_DIR` from the caller's output context and `$REPORT_CONTENT`
to the complete rendered recommendations. Never include credentials, protected
configuration or raw native errors.

```powershell
if ([string]::IsNullOrWhiteSpace([string]$ASSESSMENT_OUTPUT_DIR) -or
    -not [IO.Path]::IsPathFullyQualified([string]$ASSESSMENT_OUTPUT_DIR) -or
    -not [IO.Directory]::Exists([string]$ASSESSMENT_OUTPUT_DIR)) {
  throw "[recommend-sql-sku-sizing] PREREQUISITE_REQUIRED: An existing absolute assessment-output-dir is required."
}
if ([string]::IsNullOrWhiteSpace([string]$REPORT_CONTENT)) {
  throw "[recommend-sql-sku-sizing] REPORT_INVALID: Complete verified recommendation content is required."
}
$destination = Join-Path $ASSESSMENT_OUTPUT_DIR 'sku-assessment.md'
try {
  [IO.File]::WriteAllText($destination, [string]$REPORT_CONTENT, [Text.UTF8Encoding]::new($false))
} catch [IO.IOException] {
  throw "[recommend-sql-sku-sizing] REPORT_PUBLICATION_FAILED: The SKU output could not be saved. Prepare a writable output destination."
} catch [UnauthorizedAccessException] {
  throw "[recommend-sql-sku-sizing] REPORT_PUBLICATION_FAILED: The SKU output could not be saved. Prepare a writable output destination."
}
if (-not [IO.File]::Exists($destination) -or (Get-Item -LiteralPath $destination).Length -eq 0) {
  throw "[recommend-sql-sku-sizing] REPORT_PUBLICATION_FAILED: The saved SKU output could not be verified."
}
```

After successful writing and verification, return only a plain JSON object with
`status` equal to `completed` and a concise nonempty `reason` confirming the saved
results for the offline caller. Do not add prose or Markdown fences.
