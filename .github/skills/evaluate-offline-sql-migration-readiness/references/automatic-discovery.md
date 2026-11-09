# Automatic Application SQL Discovery

When assessment context omits `instanceName`, invoke the existing packaged
resolver before [per-instance execution](command-execution.md).
Do not treat the omission as an input failure before discovery.
An explicit instance preserves the single-instance selection and authentication
rules.

## Supported evidence

The resolver reads application JSON, XML/.config, YAML, properties, INI/.env and
statically identifiable source settings, including referenced environment values
and literal fallbacks. It does not execute application code or expressions.
Unresolved templates and placeholder hosts are not targets. Generated output,
dependency folders, tests, documentation and filesystem links are excluded.
Application facts may guide investigation but must not supply credentials.

All distinct concrete endpoints are assessed. Duplicate references are collapsed
case-insensitively; complete credential-bearing connections take precedence over
endpoint-only references. Conflicting credential configurations fail explicitly.
No target produces actionable `MISSING_PARAMETERS`, not a guessed endpoint.
Recognized Azure SQL endpoints produce `UNSUPPORTED_SCOPE`; do not reinterpret
an Azure request as offline.

## Skill-led execution

Call `scripts\Resolve-SqlAssessmentTargets.ps1` with `WorkspacePath` and, when
supplied, `ConfigurationFile`; explicit non-ConfigFile authentication selects
`EndpointOnly`. Retain its non-secret returned target selectors and private paths.
Follow the short batch commands in the execution reference. Keep resolution,
all target attempts and discovery-config cleanup in the same local tool session
with `try/finally`. Never print private configuration or generate a workflow wrapper.

Complete supported credentials are reused locally; otherwise Windows
authentication is selected before connection. Explicit authentication overrides
discovered credentials; failed credentials never trigger an alternate mode.

One target uses the exact host output directory. Multiple targets use isolated
`instances\0001`, `instances\0002`, etc. below it. Each native execution retains
its own freshness and single-connection provenance validation.

Individual failures do not prevent attempting the remaining targets within the
shared seven-minute budget. Any failure or timeout prevents completed batch
publication. The final outcome must retain each failed target's ordinal,
diagnostic code, stage, observed exit code when available, and corrective action.
Successful native artifacts are retained but are not a completed aggregate.

## Rendering and publication

After a successful call, render the complete readiness template for every
returned result. One HTML report remains byte-identical to native HTML. Multiple
reports are embedded in sandboxed script-enabled frames by the existing batch
publisher. No companion files or host changes are needed.

Per-instance execution already includes sizing using the selected mode and
generates the native combined report. Do not invoke sizing again, forward
credentials or guess performance-data mappings. No requested sizing failure may
publish a completed combined entry point.

Markdown includes the full original readiness template and requested SKU results
for every instance, not only the last. HTML preserves suitability and sizing in
the native combined reports according to [publication](report-publication.md).
