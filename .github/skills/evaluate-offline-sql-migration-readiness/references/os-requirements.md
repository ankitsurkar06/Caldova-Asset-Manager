# Guide: Host-OS Requirement (Local assessment is Windows-only)

Local assessment — readiness (`evaluate-offline-sql-migration-readiness`) and SKU sizing (`recommend-sql-sku-sizing`) — shells out to `az datamigration` / `SqlAssessment.exe`, which has Windows-only dependencies and **cannot run on Linux or macOS**.

> Azure assessments are cross-platform and are **not** subject to this check. Apply this guard **only** to the local path.

## The check (after base input validation, before tooling or SQL execution)

```powershell
if ($IsWindows -or $env:OS -eq 'Windows_NT') { 'Windows' } else { 'NotWindows' }
```

- If the result is `Windows` → continue.
- If the result is `NotWindows` (Linux or macOS), stop immediately without asking
  for inputs or running `az datamigration`. Report:

  `UNSUPPORTED_HOST`: Local assessment is only supported on Windows. Use a Windows
  machine that can reach the SQL Server.

Report an unsupported environment, not successful readiness. Terminate without
installing tools or suggesting interactive input. This guard checks native tool
compatibility only; it is not cloud/CCA detection.
