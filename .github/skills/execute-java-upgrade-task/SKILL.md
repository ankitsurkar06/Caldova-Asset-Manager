---
name: execute-java-upgrade-task
description: Execute a Java upgrade task as part of a modernization plan
---

<!-- Body is auto-populated from upstream microsoft/modernize-java at build time -->


---

## Working Folder and Reporting

When invoked from the appmod CLI orchestrator, the calling prompt will provide a
`modernization-work-folder` and a `TaskId`. The following rules are MANDATORY and
override any conflicting working-folder convention from the sections above:

- Use `${modernization-work-folder}` as the working directory for ALL bookkeeping
  artifacts (plan notes, progress logs, intermediate results). Do NOT create or
  write into any folder outside `${modernization-work-folder}` for these artifacts
  (e.g., do NOT use `.github/java-upgrade/<timestamp>/` or any other ad-hoc
  location). Source-code edits inside the target repository are unaffected by
  this rule.

- Before returning, you MUST:
  1. Create `${modernization-work-folder}/${TaskId}/` if it does not exist.
  2. Write `${modernization-work-folder}/${TaskId}/modernization-summary.md` with:
     - `finalStatus`: one of `"success"`, `"failed"`, `"skipped"`
     - `successCriteriaStatus`: object with boolean `passBuild`,
       `generateNewUnitTests`, `passUnitTests`
     - `summary`: short prose summary of what changed
     - `failureReason`: short prose, only when `finalStatus` is `"failed"`
  3. Return the same `finalStatus` / `successCriteriaStatus` / `summary` in your
     final message so the orchestrator can update `tasks.json`.
