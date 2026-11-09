---
name: recommend-sql-sku-sizing
description: "Supporting workflow for offline SQL readiness assessments: interpret Azure SQL SKU and cost recommendations generated from source configuration by default, or supplied completed performance data."
allowed-tools: PowerShell Grep View
metadata:
  modernize-selectable: "false"
---

# Recommend SQL SKU Sizing

## When to Use

Use only when delegated by `evaluate-offline-sql-migration-readiness` for its
included SKU recommendations. This is not
a standalone assessment entry point. Preserve the caller's instance, output
directory and supplied data path; do not infer a local endpoint from an Arc name.

New performance-data collection is unsupported. Never independently connect to SQL Server,
resolve SQL credentials, start a collector, create a background worker or offer
collection as a fallback. Reject a request to collect new data even if it supplies
a duration or authentication selectors.

## Workflow

### Step 1: Validate Inputs

Open [input validation](references/input-validation.md). Require `instanceName`
and use the supplied `assessment-output-dir`. Reject unsupported collection
intent and invalid, conflicting or sensitive inputs first. If `instanceName` is
missing, report `MISSING_PARAMETERS` and stop before tooling or file lookup.
Never ask questions or request another output directory.

### Step 2: Check Tools

Apply [OS requirements](references/os-requirements.md) and
[tool prerequisites](references/assessment-prerequisites.md).
No independent installation, download, upgrade, login or credential resolution is allowed.
The caller owns native-package freshness checks and refresh.
The caller owns configuration-based SQL access; sizing from completed performance
data needs neither SQL access nor Azure login.

### Step 3: Use the Validated Results

Follow [command execution](references/command-execution.md). The caller's protected
per-instance execution generates all three target reports and the combined HTML. Use its
validated `SkuJsonPaths` and `SizingCriterion`; do not regenerate results.
No performance path selects configuration-based sizing, not a local CSV search.
An explicit path selects performance-based sizing and must contain completed
matching counters. Invalid supplied data fails without fallback or collection.

### Step 4: Save Recommendations for the Caller

Render [assessment output](references/assessment-output.md) from the actual SKU
recommendation data. Preserve the full compute, storage and cost details; use
"Unavailable" for fields not present in the evidence. Never invent readiness.
Preserve verified native no-recommendation outcomes and reasons. Missing target
reports are `RECOMMENDATION_FAILED`; never invent a recommendation.

Follow [report publication](references/report-publication.md) to save
`sku-assessment.md` and unchanged native JSON/HTML in the caller's output directory.
Do not create `index.html` or `index.md`, or generate custom HTML. The offline
caller owns the report entry point and publication of the combined assessment.

## Check it worked

- Reports belong to the requested instance and cover all three targets.
- The sizing criterion is explicit; configuration estimates are not measured performance.
- Generation produced fresh usable recommendations, not just an exit code.
- The original input folder was not modified.
- No collector, independent SQL connection, credential lookup or background process ran.
- Original native artifacts and complete recommendation details were retained.

## Error Handling

Report the diagnostic code and safe corrective action in the final outcome,
rather than leaving them only in tool logs. Never repeat supplied sensitive
values or raw errors.

- Missing inputs -> `MISSING_PARAMETERS`.
- Invalid or conflicting inputs -> `INVALID_PARAMETERS` or `CONFLICTING_PARAMETERS`.
- Any new-data collection request -> `COLLECTION_NOT_SUPPORTED`; prepare completed
  data outside this assessment and supply `performanceDataPath`.
- Missing completed performance data -> `PERFORMANCE_DATA_REQUIRED`.
- Unsupported host or missing tools -> `UNSUPPORTED_HOST` or `PREREQUISITE_REQUIRED`.
- Unreadable data, denied file access, timeout or unusable recommendations -> the
  safe command diagnostic; stop without collecting or silently retrying.

Keep report content in the files. After verified generation and saving, return
only a plain JSON object with `status` equal to `completed` and a concise
nonempty `reason`. Failure or missing input must not claim completion.
