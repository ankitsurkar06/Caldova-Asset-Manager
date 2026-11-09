# Offline Assessment Input Validation

Validate non-secret inputs supplied in assessment context first. An explicit
instance and authentication selection take precedence. When the instance is
absent, use [automatic discovery](automatic-discovery.md) to resolve all concrete
application SQL endpoints and available credentials inside the local process.
Do not read raw connection settings into model context. Use the supplied context
for the output destination. Do not ask questions or infer scope from Azure identity.

## Required for lookup or execution

- `instanceName`: an optional explicit SQL Server data source, including a named
  instance or port when needed; not an Arc resource ID or a full connection string.
  If absent, every concrete endpoint resolved from application settings is a
  target. Discovery failure requires this input rather than guessing an instance.

Scope must be local/on-premises. Do not substitute a local connection for an Azure
request or infer an endpoint from an Arc resource name.

## Sizing mode

- `skuRecommendation=true|false`: retained for caller compatibility. Sizing is
  now included by default; this legacy selector does not disable it.
- `performanceDataPath`: an absolute Windows path to completed performance data
  for the same instance. It is an input path, not a report destination. Forward
  it unchanged to protected per-instance execution. An omitted path selects configuration-
  based sizing, not a search in the invocation directory. A supplied invalid
  path fails without fallback. See [assessment behavior](assessment-contract.md).

New-data collection, including an explicit collection duration or interval
selectors, is unsupported. Report `COLLECTION_NOT_SUPPORTED` and stop before
tooling, authentication or assessment execution. Do not start a collector or
reinterpret collection intent as permission to use older data.

## Output destination

Use the supplied `assessment-output-dir` as the report destination. It is an
invocation-specific directory. Do not append another layout or scan previous
invocations.

Bind this exact host value to `$ASSESSMENT_OUTPUT_DIR` inside the terminal call;
the command reference normalizes it to the absolute Windows `$OUTPUT_FOLDER`
used by the assessment tool. `outputFolder` is an internal tool configuration
field, not an additional required prompt parameter. Prompt values and a protected
config's stored output location must not override the host destination.

If host output context is missing, empty or invalid, stop with
`PREREQUISITE_REQUIRED`, explaining that a valid output destination is required.
Do not ask for another `outputFolder` input or choose the tool's former default folder.

Keep original generated files and the readiness template. Completed direct
readiness follows [report publication](report-publication.md).

Trim value-delimiting quotes and surrounding whitespace. Empty values count as
missing. Reject conflicting values, control characters, executable expressions
or connection strings supplied in place of these fields.

## Additionally required for a new run

Use one of the following noninteractive connection sources. A supplied
`configFilePath` unambiguously selects ConfigFile; an explicit incompatible
`authMode` conflicts rather than overriding it.

| `authMode` | Additional non-secret inputs | Existing prerequisite |
| --- | --- | --- |
| `Windows` | None | Current Windows identity has SQL access |
| `EntraDefault` | None | Existing noninteractive Entra credential chain works with the installed tool |
| `ConfigFile` | `configFilePath` | Existing readable protected assessment config |
| `KeyVault` | `sqlUserName`, `keyVaultName`, `secretName` | Existing Azure login can read the selected SQL-password secret |
| `CredentialManager` | `credentialTarget` | Existing module and credential entry containing SQL username/password |

For automatically discovered targets, complete supported application credentials
are used through private per-target configs. Missing application credentials use
Windows authentication; a failed supplied credential must not trigger fallback.
Explicit authentication selectors override discovery.

Mode comparisons are case-insensitive. For the explicit-instance workflow,
if neither mode nor a config path is supplied when a new run is needed, select
`authMode=Windows` using the current process identity. No authentication
environment variable or Azure login is required for that default. Explicit modes
and config paths take precedence; reject conflicting metadata or invalid modes
without trying Windows. Require an absolute config path.
SKU generation from existing performance data needs no authentication selectors.
Existing suitability results may be reused. Configuration-based sizing still
requires the selected connection; performance-based sizing does not.

Do not accept passwords, tokens or connection strings in the prompt. Complete
connection strings belong only in a protected config file. Existing SQL-login
authentication remains available through ConfigFile, Key Vault or Credential
Manager, not a plaintext-password input.

Optional `trustServerCertificate=true|false` applies to generated connection
strings; preserve the original default `true`. Encryption remains enabled.
Existing config-file connection settings are preserved, not rewritten silently.

## Intent

General readiness requests reuse matching existing results first and run when
none exist. Explicit fresh/new, refresh, rerun, replace or ignore-existing intent
may bypass reuse. A failure reading existing output does not authorize replacement.
Sizing is always included after readiness. Do not collect performance data or
ask for a duration.

## Standard diagnostics

On failure, report the applicable diagnostic in the final outcome and stop.
Name each missing, invalid or conflicting field, its input location and the
expected format or correction. Do not return only a diagnostic code or generic
"missing inputs" / "inconsistent connection source" message. Use the following
field guidance; examples are synthetic non-secret selectors, never discovered values.

| Field | Where to supply or correct it | Expected format / safe example |
| --- | --- | --- |
| `instanceName` | Assessment prompt, when overriding discovery or no target can be resolved | One SQL endpoint: `instanceName=localhost`, `instanceName=SQLHOST\INSTANCE`, or `instanceName=SQLHOST,1433`; not a connection string or Arc resource ID |
| `authMode` | Assessment prompt, only when selecting a non-default mode | One of `Windows`, `EntraDefault`, `ConfigFile`, `KeyVault`, `CredentialManager` |
| `configFilePath` | Assessment prompt | Absolute Windows path to an existing readable protected JSON file, e.g. `configFilePath="C:\Secure\assessment.json"`; do not paste the file contents |
| `sqlUserName` | Assessment prompt for KeyVault | Nonempty SQL login name, e.g. `sqlUserName=assessment_reader`; never its password |
| `keyVaultName`, `secretName` | Assessment prompt for KeyVault | Nonempty names identifying the existing vault and SQL-password secret; never the secret value |
| `credentialTarget` | Assessment prompt for CredentialManager | Nonempty name of an existing Windows Credential Manager entry containing SQL username and password |
| `skuRecommendation`, `trustServerCertificate` | Assessment prompt when supplied | `true` or `false` |
| `performanceDataPath` | Assessment prompt when supplying existing SKU data | Absolute Windows directory path to completed data for the selected instance |
| `sqlAssessmentPath` | Assessment prompt only when selecting a prepared native build instead of the cache | Absolute Windows executable path containing both configuration-based sizing and combined saved-report generation |
| `forceSqlAssessmentRefresh` | Assessment prompt when explicitly forcing an official package download | Boolean; conflicts with an explicit `sqlAssessmentPath` |
| `assessment-output-dir` | Host-supplied invocation context, not a new prompt field | Valid absolute output directory; rerun through the assessment host if absent or invalid; never request `outputFolder` |
| `action` | Protected JSON file selected by `configFilePath` | JSON string `"Assess"` |
| `sqlConnectionStrings` | Protected JSON file selected by `configFilePath`, never the prompt | Nonempty collection with exactly one connection whose `Data Source` / `Server` matches prompt `instanceName`; other entries are not assessed |
| `Data Source` / `Server` | Selected connection in the protected file | One nonempty unambiguous SQL endpoint, matching prompt `instanceName` |
| `User ID` / `Password` | Selected connection in the protected file, only for SQL-login authentication | Nonempty login and password stored privately; alternatively prepare supported noninteractive Windows or Entra authentication |

`MISSING_PARAMETERS`: Missing required inputs: <field names, locations and expected formats>.

The only potentially required user-supplied base field is `instanceName`, when
automatic discovery finds no target; when needed, list the
selected mode's missing fields in table order. Omitted `authMode` and
`configFilePath` alone are not missing-parameter failures for offline readiness.

`INVALID_PARAMETERS`: Invalid inputs: <field names, locations and expected formats>.

`CONFLICTING_PARAMETERS`: Conflicting inputs: <field names, locations and correction>.
For `configFilePath` with an incompatible `authMode`, explain that the prompt
must use `authMode=ConfigFile` or omit `authMode`; do not silently switch modes.
For repeated conflicting prompt selectors, name the selector and request one
value without echoing either supplied value. For no matching protected connection,
identify prompt `instanceName` versus protected `sqlConnectionStrings` and its
`Data Source` / `Server`; correct either selector to refer to the intended same
endpoint outside assessment. For multiple matching connections, remove duplicates
from the protected file so exactly one matches. Do not display the entries.
The supplied report destination must not be overridden.

Examples of safe final diagnostics:

- `MISSING_PARAMETERS: instanceName is required in the assessment prompt; use one SQL endpoint, e.g. instanceName=SQLHOST\INSTANCE.`
- `MISSING_PARAMETERS: authMode=KeyVault requires sqlUserName, keyVaultName and secretName in the assessment prompt; supply nonempty selector names, never a password or secret value.`
- `CONFLICTING_PARAMETERS: prompt configFilePath conflicts with prompt authMode; use authMode=ConfigFile or omit authMode.`
- `AUTHENTICATION_REQUIRED: selected protected sqlConnectionStrings entry is missing User ID or Password; supply the missing nonempty SQL-login field only inside the protected file, or configure supported noninteractive Windows or Entra authentication.`

If sensitive values appear, reject them without repeating them. Explain that only
non-secret selectors or a protected config-file path are supported. Report a
failure without copying such values into a terminal command
or generated report. Never echo supplied values, protected file contents, raw
parser errors or credential-store responses in diagnostics. Name keys and use
synthetic examples only. Do not ask interactive follow-up questions during the
assessment; explain the correction for the user to make outside it, then rerun.
