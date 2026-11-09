# Protected SKU execution

The skill-led offline caller invokes a protected per-instance helper. Its bundled
`SqlAssessment.Recommendations.ps1` helper is loaded inside the same process;
never copy it into a new terminal invocation or reconstruct native commands.
Do not invoke sizing again when the caller already returned validated results.

Without `performanceDataPath`, use `ConfigBased` with the assessed connection
through a user-only temporary JSON configuration. Never pass connection strings
on the command line, read them into model context or resolve credentials in a
separate agent turn.

With an explicit `performanceDataPath`, use `PerformanceBased`. Require the
canonical instance's nonempty `_PerformanceAggregated_Counters.csv` and stage
only matching CSVs plus common CSV inputs, leaving the source unchanged. Missing,
empty or mismatched data is `PERFORMANCE_DATA_REQUIRED`, not authorization for a
configuration fallback or new collection.

Run all three targets explicitly: AzureSqlDatabase, AzureSqlManagedInstance and
AzureSqlVirtualMachine. Validate source identity, target metadata, generation
time, requirements and exact suitability database coverage. A valid explicit
no-recommendation outcome is allowed; missing reports are `RECOMMENDATION_FAILED`.

The caller then generates the native combined report from the saved suitability
and SKU JSON in an isolated folder. It verifies self-contained HTML before
publication. Exit zero alone never proves completion.
