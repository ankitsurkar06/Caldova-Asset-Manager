---
name: evaluate-offline-sql-migration-readiness
description: "Evaluate migration readiness and Azure SQL sizing for local/on-premises SQL Server dependencies detected in application code or configuration, or explicitly supplied instances. Discover all concrete SQL endpoints using available application credentials or Windows authentication. Generate configuration-based sizing by default, performance-based sizing when performanceDataPath is supplied, and combined reports for Azure SQL Database, Azure SQL Managed Instance and SQL Server on Azure VM. No performance collection."
allowed-tools: Bash(az:*) PowerShell Grep View
metadata:
  modernize-selectable: "true"
  modernize-report-title: "Azure SQL readiness"
---

# Evaluate Offline SQL Migration Readiness

## When to Use

Use for detected application SQL Server dependencies or explicitly local/on-premises readiness, including
SKU recommendations by default. Omitted `performanceDataPath` selects configuration-based
sizing; an explicit path selects sizing from completed performance data. New performance-data
collection is not supported. Preserve the supplied assessment context. Do not infer a local
connection from an Arc resource name, discover Azure resources or substitute local
results for an Azure assessment request.

This provisional skill runs unattended on Windows. Guaranteed cloud rejection
remains deferred. OS detection is a tool compatibility check, not a way to infer
CCA execution. Keep the original readiness template and native report artifacts.
Publish completed direct readiness results using the package-local
[report publication](references/report-publication.md) contract.

## Workflow

The skill orchestrates short prerequisite commands, native tool preparation,
target selection and complete-batch publication. Only protected per-instance
execution uses `scripts\Invoke-SqlReadinessAssessment.ps1`, invoked with
`pwsh -NoProfile -NonInteractive -File`. Never reconstruct credential helpers,
change `.psbase`, generate a workflow wrapper or pipe multiline code to `-Command -`.
That helper owns credential lifetime, native execution, sizing, combined-report
generation and artifact validation, not the overall workflow. Follow
the [assessment behavior](references/assessment-contract.md). Preserve its safe diagnostic code,
stage and observed native exit code; never infer that a command
failure means an installed tool is outdated. See
[command execution](references/command-execution.md) for arguments.

### Step 1: Validate Base Inputs

Open [input validation](references/input-validation.md) first. An explicit
`instanceName` restricts the target; it is not required when application settings
identify concrete local SQL endpoints.
Use the supplied `assessment-output-dir` for report files. Do not require a separate
`outputFolder` input. Validate any supplied authentication selectors, but do not
resolve credentials yet. Missing, conflicting, invalid or sensitive prompt input
reports its safe diagnostic and terminates.
Missing fields for an explicitly selected authentication mode produce
`MISSING_PARAMETERS` and stop before tool checks. An omitted instance alone
requires automatic discovery, not this early failure.

Do not override the host output destination, ask a question, infer a local connection
from an Arc resource name or read application connection settings into model context.

### Step 2: Check the Host and Tools

Apply [OS requirements](references/os-requirements.md) and
[tool prerequisites](references/assessment-prerequisites.md) using the documented
short local commands. Missing/unsupported
prerequisites stop execution without installing other tooling or signing in.
The skill checks or refreshes the official native package by default;
an explicit `sqlAssessmentPath` pins a preview build instead. Follow the
prerequisite reference for freshness, hash verification and refresh failures.

### Step 3: Resolve Targets and Check Existing Results

When `instanceName` is absent, follow
[automatic discovery](references/automatic-discovery.md). Invoke the existing
resolver, then each target helper, retaining discovery configs until all target
attempts finish and removing them in `finally` in the same local tool session.
Do not use View, Grep or terminal output to inspect raw application configuration
or connection strings. The resolver returns only non-secret endpoint selectors
and paths to user-only temporary configs. Reuse complete application credentials
inside that process; otherwise use Windows authentication. Explicit authentication
selectors override discovered credentials.

Assess every distinct resolved endpoint, not just the first one. Apply Steps 3-6
per instance, with isolated output directories for multiple targets. Retain the
original host destination for final publication. Continue other targets after an
individual failure within the shared time budget, but never publish a partially
completed batch. If no target can be resolved, report `MISSING_PARAMETERS` with
guidance to supply `instanceName` or concrete application settings.

For a general request, the per-instance helper follows [command execution](references/command-execution.md)
to find usable JSON for the exact supplied instance in the host output directory. Render
matching existing suitability results with their generation time. Sizing and
combined generation still run for the selected mode. Do not ask whether to refresh.

Only explicit fresh/new, refresh, rerun, replace or ignore-existing intent supplied
before invocation may bypass reuse. A read/parse failure or orphaned HTML report
is not "no results" and must not trigger a replacement automatically.

### Step 4: Run a New Assessment

For each target, if no matching result exists, or fresh execution was explicitly requested, finish
the conditional authentication-input checks in
[input validation](references/input-validation.md). If neither `authMode` nor
`configFilePath` is supplied, use `authMode=Windows` with the current process
identity. Explicit connection sources take precedence; missing fields for a
selected mode produce `MISSING_PARAMETERS` and stop before credential resolution
or SQL connection. Invalid/conflicting inputs must not select the default.

The entry point implements [local connection](references/local-server-connection.md)
and the command reference. Do not resolve credentials or reconstruct command
blocks yourself. Never read a credential file into model context, print credentials,
pass a connection string on the command line or request a password in the prompt.

Use existing Windows/Entra identity or the specified config/credential store.
Unavailable authentication, including failed default Windows authentication,
fails gracefully; do not sign in, install modules or try another mode. A new run
must complete successfully and produce matching fresh
JSON. An IP address or DNS alias may differ from the report's canonical SQL name.
For fresh execution, bind the result to the single requested connection and the
unique report newly written by that run, as specified in the command reference.
Reused reports still require an exact instance-name match. Do not render stale,
ambiguous or partial output as a successful new assessment.

### Step 5: Render Readiness

Read the validated assessment JSON, then render
[readiness output](references/readiness-output.md) using its complete original
template. Include all three targets and the actual generated report path, preferring
the matching HTML file when present and JSON otherwise. Preserve user-database
counting and interpretation rules. Show the generation time for reused results.

Do not substitute a prose summary, shorten readiness for SKU guidance or change
the template after a retry.

### Step 6: Include Sizing and the Combined Report

The per-instance helper runs `recommend-sql-sku-sizing` execution while its
protected connection remains alive. Do not run sizing again or delegate credentials.
The existing `skuRecommendation` option remains accepted, but sizing is included
even when it is absent or false. No `performanceDataPath` means `ConfigBased`;
a supplied path means `PerformanceBased`. Invalid supplied data fails without
fallback. Never collect performance data.

Use the supporting `recommend-sql-sku-sizing` skill to interpret the validated
`SkuJsonPaths` and render the complete recommendation template when needed.
Clearly label the sizing criterion; configuration estimates are not observed
performance. A verified native no-recommendation outcome is valid evidence, not
permission to invent prices or capacity.

For HTML, the skill calls the existing publisher only after every target succeeds,
preserving one native combined report unchanged or embedding all instances' reports. Each `HtmlPath`
identifies that combined report; `SuitabilityHtmlPath` retains readiness-only
provenance. Do not defer publication merely because sizing is included.

For Markdown, append the complete SKU recommendation template after the
unchanged readiness template for each instance, then follow
[report publication](references/report-publication.md). Preserve native artifacts.
If sizing or combined generation fails, do not publish an entry point or claim
the combined request completed. Do not publish readiness-only HTML as a fallback.

## Check it worked

- Existing JSON belongs to the requested instance; filename similarity alone is
  not sufficient.
- A new run produced exactly one fresh, usable single-server JSON tied to its
  single requested connection; endpoint aliases may differ from the report name.
- The complete original readiness response and correct generated report link
  were retained.
- Direct HTML publication preserves the verified native report byte-for-byte as
  `index.html`; Markdown publication preserves the full readiness template.
- No password, token, connection string, protected config content or raw CLI
  error was returned to the model or included in the final response.
- The temporary credential config was deleted on success/failure. Retain the
  user's original config and assessment output.
- Discovery assessed all distinct concrete endpoints, reused credentials only in
  the local process, and did not publish a completed batch after any target failed.
- All three SKU target reports match the assessed source and database inventory;
  the aggregated "Azure SQL readiness" link opens suitability and sizing together.

## Error Handling

- Input failure -> use the [input diagnostics](references/input-validation.md)
  and stop, naming fields rather than raw supplied values.
- New-data collection request -> `COLLECTION_NOT_SUPPORTED`; stop before tools
  or authentication, even when a duration or connection selectors were supplied.
- Unsupported host/missing tooling -> use the prerequisite diagnostic and stop;
  no installation of CLI tools, extensions or modules inside this invocation.
- Native package check/download failure -> report `PACKAGE_DOWNLOAD_FAILED` or
  `PACKAGE_INVALID`; do not run a stale cached executable.
- Unavailable credentials/login -> emit `AUTHENTICATION_REQUIRED` with preparation
  guidance outside assessment. Never request a secret or fallback authentication.
- Denied access -> emit `ACCESS_DENIED`; the user must prepare SQL/store/file
  permissions outside this invocation.
- Network, certificate or assessment failure -> report the safe cause and stop;
  do not disable encryption, switch instance or silently retry.
- Invalid/missing JSON or timeout -> do not render readiness conclusions from
  incomplete or stale evidence.

Honor cancellation and the ten-minute invocation limit. No detached work,
collectors or follow-up questions. Report the diagnostic code, missing input
names and safe corrective action in the final outcome rather than leaving them
only in tool logs. Missing/invalid inputs and unmet prerequisites require
additional input; unsupported environments and execution failures must not be
presented as successful assessments. Never echo supplied values, credentials or
raw errors.
Do not introduce or repeat the diagnostic or render a successful assessment
template for an unsuccessful outcome.

## References

- [Input validation](references/input-validation.md)
- [Automatic discovery](references/automatic-discovery.md)
- [Local server connection](references/local-server-connection.md)
- [OS requirements](references/os-requirements.md)
- [Assessment prerequisites](references/assessment-prerequisites.md)
- [Command execution](references/command-execution.md)
- [Readiness output](references/readiness-output.md)
