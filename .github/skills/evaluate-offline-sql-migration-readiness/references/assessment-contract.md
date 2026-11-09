# SQL assessment behavior

The skill directs prerequisite checks, tool preparation, endpoint discovery,
per-instance execution and final publication. Only the credential-bearing
execution and result acceptance boundary must remain inseparable: credentials
stay private until suitability and sizing finish, and successful process exit
alone never establishes a completed assessment. Discovery credentials remain
private until all selected instances have been attempted, then are removed even
when execution fails. This boundary does not require a packaged batch pipeline.

Every successful assessment includes suitability, sizing for Azure SQL Database,
Azure SQL Managed Instance and SQL Server on Azure VM, and a native combined
report. The existing supporting sizing option remains accepted; it is not
required to obtain sizing.

Without an explicit performance-data location, sizing uses source configuration.
These estimates assume source capacity rather than measured workload utilization.
With an explicit location, sizing uses completed instance-specific performance
data. Invalid supplied data fails rather than falling back to configuration.
Neither mode collects performance data.

Application endpoint discovery, explicit target precedence and noninteractive
authentication remain unchanged. Configuration-based sizing reuses the assessed
connection privately. Performance-based sizing does not require SQL credentials
when suitability results can be reused.

Reusable suitability results do not prove sizing completion. Sizing and combined
report generation use an isolated new result set for the selected mode. All three
target reports must identify the assessed source and its database inventory.
A valid native no-recommendation outcome is retained, not replaced with invented
capacity or prices. Missing target results are not a completed assessment.

The native tool must support both new capabilities and saved-report metadata.
Before using the default tool, check the current Microsoft download-center
package. Reuse a cached archive only when its strong remote identity and local
content hash still match; otherwise download a fresh package. An explicit refresh
forces a new download even when the remote identity is unchanged. Check failures
must not silently use stale binaries.

Extract each invocation's package separately so concurrent callers and other
applications are not affected by replacement of shared executables. Remove the
invocation's extracted files after execution. Keep only the verified download
cache. Other tools and extensions are never installed or upgraded.

An explicitly selected prepared build is pinned for preview testing and does not
get replaced by the download-center package. Unsupported published or preview
tools, invalid downloads, timeouts and cleanup failures are explicit failures.

Publish only after every selected instance has completed. The aggregated report
continues to show "Azure SQL readiness"; its destination contains suitability
and sizing together. Single-instance HTML preserves the combined native report
unchanged. Multiple instances use self-contained embedded native reports.
Markdown includes the full suitability and sizing results. Never publish secrets,
external-asset dependencies, stale fallback reports or a partial successful batch.
