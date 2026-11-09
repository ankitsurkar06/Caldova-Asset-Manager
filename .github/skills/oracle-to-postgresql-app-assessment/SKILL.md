---
name: oracle-to-postgresql-app-assessment
description: >-
  Assess Oracle-dependent application code for migration to PostgreSQL. Select when
  active application dependencies, Oracle JDBC or .NET providers, Oracle APIs, SQL,
  or ORM configuration demonstrate Oracle usage. Requires Node.js 22+ and a readable
  workspace. Do not select for documentation-only mentions, passive Oracle JARs,
  extracted DDL without active application consumers, PostgreSQL-only applications,
  database-server assessments, or non-Oracle migrations.
metadata:
  modernize-selectable: "true"
  modernize-report-title: "Oracle to PostgreSQL App Readiness"
---

# Offline Oracle Application Assessment for Modernize

This is a Modernize host adapter, not the standalone interactive migration workflow.
Never invoke the standalone skill, its runtime finalizer, its report validator, or its
PostgreSQL provider-discovery workflow. No PostgreSQL MCP or live connection profile is used.

## Inputs

- Required: `workspace-path`, `assessment-output-dir`, `report-format` (`html` or `markdown`).
- Optional explicit adapter inputs: `postgres-ddl-folder`, `oracle-ddl-folder`, `oracle-schema`.
- Otherwise read the optional application repository configuration described in
  [host-contract.md](./references/host-contract.md).
- Optional user context describes the assessment goal or focus only. It does not enable
  migration options, supply credentials, or configure DDL paths.

## Execution

1. Read [host-contract.md](./references/host-contract.md) and
   [offline-ddl.md](./references/offline-ddl.md). Check Node.js 22+ before expensive work.
   Never install tools, request input, open authentication, or wait for confirmation.
2. Run the packaged helper, using its package-relative location:

   ```text
   node "<skill-directory>\scripts\modernize-assess.mjs" --workspace-path "<workspace-path>" --assessment-output-dir "<assessment-output-dir>" --report-format "<report-format>"
   ```

   Append `--postgres-ddl-folder`, `--oracle-ddl-folder`, or `--oracle-schema` only when
   that explicit adapter input was actually supplied. The repository configuration works
   with the current Modernize CLI without any new host flags.
3. The helper performs bounded static scope detection, excludes host artifacts and configured
   DDL evidence folders from application evidence, resolves configuration, optionally indexes
   PostgreSQL DDL, and writes only the requested direct-child `index.html` or `index.md`.
   It does not change application source, build, test, measure coverage, start Docker, use MCP,
   connect to a database, or write a database.
4. Do not run the full standalone assessment afterward. Do not add sibling assets or links,
   copy raw source/DDL into the report, or turn static evidence into a runtime claim.
   The generated report is self-contained. Preserve its validation boundaries.
5. If the helper reports `skipped`, return that outcome and do not manufacture a report.
   If scope is incomplete, retain the generated limitations report; do not recommend conversion.
   An explicitly supplied invalid DDL folder is an error, never an implicit code-only fallback.
6. Finish within **10 minutes** total and honor cancellation. The helper has stricter internal
   scan, input-size, and time budgets. Do not retry an unchanged failure or begin unbounded work.

## Completion

Clearly report whether the requested report was generated, the assessment was not applicable,
or execution failed, with a concise non-secret reason. A missing required prerequisite is
reported immediately, not requested interactively. Report generation does not establish
conversion or production readiness. Never include source, DDL, secrets, connection information,
or input values in the outcome message. Modernize manages the final response format.
