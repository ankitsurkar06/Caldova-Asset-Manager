# External secrets and administrator recovery

## Scope and compatibility

This is a transitional implementation on the actual .NET Framework 4.7.2 /
MVC5 / OWIN / ASP.NET Identity / EF6.5.1 runtime, **not** a .NET 10 port.
Azure.Security.KeyVault.Secrets4.6.0 uses the existing Azure.Identity1.11.4 and
Azure.Core1.38.0. A future runtime migration must carry this boundary to its
host configuration and validate it again. No legacy configuration builder is
installed; no Key Vault client secret or developer credential chain is used.

SQL remains managed-identity-only under [SQL-MANAGED-INSTANCE.md](SQL-MANAGED-INSTANCE.md).
Do not store a SQL password in Key Vault. Blob migration is separately blocked;
this task adds no storage credentials, assets or public access.

## Public configuration and secret references

Only **public settings and secret names**, never secret values, belong in
Web.config or host environment variables. Host process environment overrides
Web.config: prefix `CALDOVA_`, replace dots with underscores and uppercase.
An explicitly empty environment value does not fall back to a file value.
Do not configure `Authentication.*.Secret` or `MachineLearning.AccountKey`:
these old inline secret paths are no longer read.

| Logical setting | Environment variable | Purpose |
|---|---|---|
| `KeyVault.Uri` | `CALDOVA_KEYVAULT_URI` | Vault root, e.g. `https://<vault-name>.vault.azure.net/` |
| `KeyVault.ManagedIdentityClientId` | `CALDOVA_KEYVAULT_MANAGEDIDENTITYCLIENTID` | Optional user-assigned identity **client GUID**; omit/empty for system-assigned |
| `Authentication.Facebook.Enabled` | `CALDOVA_AUTHENTICATION_FACEBOOK_ENABLED` | Explicit `true`/`false`; default false |
| `Authentication.Facebook.Key` | `CALDOVA_AUTHENTICATION_FACEBOOK_KEY` | Public provider application/client ID |
| `Authentication.Facebook.SecretName` | `CALDOVA_AUTHENTICATION_FACEBOOK_SECRETNAME` | Name of secret in the vault, not a value or full secret URL |
| `ShowRecommendations` | `CALDOVA_SHOWRECOMMENDATIONS` | Shared controller/UI/client switch; default false |
| `MachineLearning.AccountKeySecretName` | `CALDOVA_MACHINELEARNING_ACCOUNTKEYSECRETNAME` | Name of legacy recommendation API credential in vault |

Repeat the three Facebook settings for **Google**, **Microsoft** and **Twitter**
with the corresponding name in the logical/environment setting. All four
providers remain disabled in source. Empty former placeholders were not leaked
credentials. Explicit enabling now requires a public key and secret-name reference;
setting only a public key does not enable a provider.

Vault roots must use HTTPS port443, no credentials, query, fragment or non-root
path. Supported host suffixes are `.vault.azure.net`, `.vault.azure.cn` and
`.vault.usgovcloudapi.net`. Use the service vault hostname even with private
endpoints; configure DNS externally. Secret names are 1–127 letters, digits or
hyphens. Do not put credentials, tokens or URIs into name settings.

## External operator prerequisites — not executed by this change

1. Identify the intended host managed identity. Enable/attach it externally.
   With a user-assigned identity, provide its client ID, not object/resource ID.
   No `DefaultAzureCredential`, Azure CLI, Visual Studio, service-principal
   password or interactive login fallback is allowed by this code.
2. An authorized operator must populate the intended vault with the actual
   required provider/API secrets, using an approved secure channel. Do not pass
   values through source control, shell history, command logs, tickets or docs.
3. Grant only required secret-read access to that identity, e.g. Key Vault
   Secrets User scoped according to policy. Do not grant Secrets Officer,
   write/delete/purge or subscription administration to the web identity.
   Verify RBAC/access-policy mode, tenant, firewall/private endpoint, DNS and
   outbound HTTPS from the intended host externally.
4. Supply the public settings and secret names through the host. Keep unused
   features disabled. Restart/recycle deliberately after configuration changes.
   Validate in a controlled environment before enabling provider traffic.
5. Existing social-provider registrations, callback URLs, linked user IDs,
   Identity cookies, security-stamp checks, roles/claims and authorization remain
   unchanged. This is **not** Entra end-user sign-in migration.

Enabled social credentials are read during OWIN setup; the API credential is
read when Unity constructs the singleton HTTP client during startup. Missing,
invalid, empty, inaccessible, disabled or expired required secrets cause an
explicit configuration exception, rather than silently disabling the feature
or falling back to a plaintext password. Disabled features do not contact
Key Vault or need its URI/identity settings. The recommendation controller and
UI use the same host-aware switch as the HTTP client.

Secret lookups are versionless, with one SDK retry, a 10-second per-network
timeout and a 30-second cancellation budget for secret retrieval. SDK content
and identity logging are disabled on these clients. Failures are converted to
generic configuration errors with no value, endpoint, response body or original
inner exception. Diagnose via approved external Azure identity/vault diagnostics;
never log resolved secrets, Authorization headers or raw SDK exceptions.
Do not enable body/header tracing in production. The application does not
implement periodic secret refresh: restart **all** instances after rotation.

Recommendations remain disabled. When externally enabled, the existing API
Basic-header byte encoding/contract is retained; the endpoint and its historical
HTTP failure handling are owned by later tasks. No claim is made that that
legacy service still exists or that enabling it is currently safe.

## Rotation and historic exposure response (mandatory external action)

Deleting a value from current source does **not** revoke it or erase repository
history, published packages, logs or database accounts. Treat the former
embedded administrator password as exposed, including any reuse elsewhere.
Never reproduce it to identify affected accounts.

Two nonempty password defaults were also removed from the historical
`IaaS2PaaSWeb/Environments/WorkshopEnv.json` securestring parameters
`AdminUserPassword` and `dbUserPassword`. Those legacy parameters now require
external secure input; this is **not** approval to deploy that obsolete IaaS
template or use its SQL-password scripts with the managed-identity application.
Rotate every VM/local administrator/SQL account where those defaults were used.
Existing generated `Environments/bin/Debug` and staging copies can still contain
the old defaults; treat those outputs and old deployment packages as sensitive,
quarantine/remove them through the approved artifact process and never publish
them. Source removal is not artifact cleanup or issuer/account revocation.

- Identify every account/environment where it was used; rotate/reset through
  approved identity administration. Revoke active sessions and update security
  stamps as appropriate. Review privileged access and remove unintended
  demonstration accounts under an approved process, not a startup script.
- Rotate any provider/API credentials actually used outside this repository,
  independently of the empty placeholders. Populate a new vault secret version
  through a secure operator channel. Coordinate old/new validity overlap with
  the provider, recycle all application instances, validate sign-in or API
  behavior externally, then revoke the previous credential at its issuer.
  Deleting a vault version alone does not revoke the provider credential.
- If access fails, preserve the sanitized failure, correct identity/RBAC/network/
  secret state, and restart. Do not restore exposed passwords, inline secrets,
  broad vault permissions or developer-login fallbacks as a rollback.

No issuer revocation, account rotation, vault upload or session invalidation
has been executed by this code change. Operators must record these separately.

## Safe administrator bootstrap / recovery

There is no administrator password setting, default privileged user or web
startup bootstrap. Normal sign-in/registration and authorization continue; an
ordinary registration must never auto-grant administrator privileges.

Preserve migrated Identity **user IDs, password hashes, linked external logins,
security stamps, role membership and claims**. Existing administrators should
continue using those records after the controlled data migration.

If there is no recoverable administrator, use a separately approved, audited,
one-time **external administrative process** with a narrowly authorized identity
and access to the correct database. Verify the intended person's identity,
create/recover the account with a unique securely delivered credential, apply
only the established required role/claims, verify effective authorization and
revoke the provisioning access immediately. Use ASP.NET Identity-compatible
password hashing/token/security-stamp operations; never write plaintext or
hand-made password hashes. Do not give the runtime identity DDL/bootstrap
privileges, provision at every web startup, publish a bootstrap endpoint or
check a provisioning password into source/host app settings. No bootstrap tool
or live account changes are included here.

## Validation boundary

Local builds and offline configuration/error-path checks are evidence only for
code behavior. Real managed-identity token acquisition, Key Vault RBAC/network/
secret state, external sign-in, issuer rotation and administrator recovery
require external controlled validation. See task `005-summary.md` in the
modernization report folder for exact local results and outstanding blockers.
