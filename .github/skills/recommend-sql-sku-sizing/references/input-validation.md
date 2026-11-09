# SKU Inputs

Use only the offline caller's validated non-secret context. Do not ask questions,
infer SQL identity from repository configuration or Arc names, or resolve
credentials. Reject passwords, tokens and connection strings without echoing them.

## Required context

- `instanceName`: the assessed local SQL instance.
- Supplied `assessment-output-dir`: the offline caller's exact invocation
  destination. Require an absolute Windows path; do not choose another folder.
- The caller's validated target reports and sizing criterion.

Missing `instanceName` is `MISSING_PARAMETERS`. Invalid or absent output context
is `PREREQUISITE_REQUIRED`. Conflicting instance or source scope is
`CONFLICTING_PARAMETERS`; do not substitute local results for an Azure request.

## Existing performance data

Optional `performanceDataPath` is an absolute Windows path to the exact completed
data folder. It is an input location, not a publication destination. Without it,
use configuration-based sizing through the caller's protected process context.
Never scan default folders,
parents, previous runs or unrelated instances.

Require the instance's nonempty aggregated time-series counters CSV. Static files
alone, partial live data or a generic SKU report filename do not prove completed
performance data. Do not monitor, resume or complete an active collection.
Copy matching data to the caller's destination without modifying the source.
Generate fresh SKU recommendations from those existing inputs.

Performance-based sizing needs no SQL authentication selectors or Azure login.
Configuration-based sizing reuses the caller's assessed connection; never
forward credentials to the supporting skill.

## Collection is unsupported

Any request to collect new data must stop with `COLLECTION_NOT_SUPPORTED`,
including `collectionDuration`, `collectionDurationSeconds`,
`perfQueryIntervalSeconds`, `staticQueryIntervalSeconds`, `numberOfIterations`
or equivalent natural-language collection intent. A supplied duration never
authorizes a collector. Do not create a worker, tracking/status files or background
process. Only the caller's packaged process may manage private connection files.

## Diagnostics

Report the diagnostic in the final outcome and stop, naming canonical fields
rather than supplied values. Never expose raw errors.

- `MISSING_PARAMETERS`: Missing required inputs: instanceName.
- `INVALID_PARAMETERS`: Supply valid non-secret instance and absolute path inputs.
- `CONFLICTING_PARAMETERS`: Specify one consistent local SQL instance and scope.
- `COLLECTION_NOT_SUPPORTED`: New performance-data collection is not supported.
  Prepare completed data outside this assessment and supply performanceDataPath.
- `PERFORMANCE_DATA_REQUIRED`: Completed instance-specific aggregated performance
  data is required. Supply performanceDataPath for data prepared outside assessment.

Missing inputs/data and unavailable prerequisites require additional input.
Unsafe input, unreadable/invalid data or failed execution must not claim success.
