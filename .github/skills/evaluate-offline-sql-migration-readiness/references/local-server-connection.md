# Noninteractive Local SQL Connection

Complete [input validation](input-validation.md), then invoke the packaged
[execution entry point](command-execution.md). All existing authentication
mechanisms remain available. Never ask for credentials, accept them in a prompt,
open interactive sign-in, or reconstruct the connection helpers.

The packaged helpers retain `DbConnectionStringBuilder.psbase.ConnectionString`.
PowerShell's dictionary adapter makes assignment without `.psbase` behave
differently; do not copy, simplify or rewrite this code.

## Credential ownership

- Explicit `instanceName` restricts the target.
- A protected ConfigFile must contain `action` equal to `Assess` and a nonempty
  `sqlConnectionStrings` collection with exactly one endpoint matching the
  explicit instance, case-insensitively and including port or `tcp:` prefix.
  Equivalent endpoint spellings are not normalized during explicit selection.
- Without an explicit instance, the packaged discovery resolver reads supported
  application settings locally and produces private per-target configs.
- Complete supported application credentials are reused locally. When absent,
  use the current Windows identity before connection; never retry failed
  application credentials with Windows.
- Explicit authentication selections override discovered credentials.
- Windows uses the current process identity. EntraDefault uses an existing
  noninteractive credential chain. KeyVault uses an existing authorized session
  to read a password secret; the SQL username is a separate non-secret selector.
  CredentialManager requires an existing accessible entry and installed module.
- A supplied protected file is never edited. Stored output locations cannot
  override the host's invocation directory.

## Privacy and cleanup

Per-instance temporary-config creation, execution and cleanup occur in one
protected process. Discovery configs remain private across per-instance calls;
the skill removes them after all target attempts in its local tool session.
User-only ACLs are applied before credentials are written.
Temporary files are removed on normal completion, handled failure and timeout;
cleanup failure is explicit. Abrupt external process termination can bypass
PowerShell cleanup and requires inspection outside the assessment.

Never View protected configs, display environment connection values, print raw
native output, or pass credentials/connection strings as process arguments.
The entry point checks fresh native artifacts for the selected connection's
password/token before returning them for model consumption or publication.
Sensitive output is invalid, not publishable.

Unavailable authentication, invalid settings and denied access produce distinct
safe diagnostics. Correct the named settings or permissions outside assessment;
never supply the protected file contents or password in the prompt.
