# Task 005 — scoped execution plan

- Task: `005-transform-credentials-keyvault`; date: 2026-10-23.
- Workspace: `C:\Users\ankitsurkar\Caldova-Asset-Manager`.
- Supplied branch: `modernize/dotnet-20261023211948`; no branch manipulation.
- Session ID: unavailable; discovery for appmod-run-task returned names without callable definitions. No migration context/session was fabricated.
- Authority: confirmed `plan.md` and `.metadata/tasks.json`, task 005. Do not rewrite metadata schema.
- Dependencies: 001 (.NET 10 port) and 004 remain blocked; retain feasible 002/003 net472, EF6.5.1, Microsoft.Data.SqlClient5.2.3 and secretless MI/no-startup-seeding work. Overall task acceptance remains pending.

## Scope and sequence

1. Read current credential consumers, startup, project/packages and configuration. Inspect status only through appmod version-control if callable; otherwise record unavailable, preserve files without version-control operations.
2. Dependencies: use existing Azure.Identity1.11.4/Azure.Core1.38.0; add compatible Azure.Security.KeyVault.Secrets4.6.0 and explicit legacy project reference. Do not broadly upgrade packages or run CVE remediation. Seek .NET dependency guidance via MCP; record unavailable definitions.
3. Configuration: remove administrator bootstrap settings and obsolete inline provider/API secret settings. Add explicit disabled social-provider switches, external public client IDs and secret-name references. Keep ShowRecommendations=false. Use host environment overrides and compatible net472 settings, not a fictitious ASP.NET Core/appsettings migration or legacy config builder.
4. Code:
   - New external-settings/Key Vault boundary using Azure SDK SecretClient and ManagedIdentityCredential only (optional user-assigned client ID), HTTPS Azure vault URI/name validation, bounded requests and sanitized errors with no inner SDK exceptions or secret values.
   - Existing ConfigurationLoginProviderCredentials reads explicit enabled switch; disabled providers make no vault call. Enabled providers require public client ID and vault secret reference. Preserve OWIN registration, cookie/Identity/sign-in/authorization semantics.
   - HttpClientWrapper obtains MachineLearning API credential from vault only when recommendations are enabled; disabled construction never requires secrets. Keep Basic authentication contract; do not change endpoint/HTTP modernization owned by later tasks.
   - Secret-related configuration parse errors must not echo values.
5. Documentation: add `docs/KEY-VAULT-SECRETS.md` covering external settings, managed identity/RBAC/network prerequisites, safe rotation/restart, revoked historic administrator password, controlled audited external bootstrap and preservation of existing Identity records/roles/claims. No provisioning or bootstrap execution.
6. Verification: first seek required MCP build/test/CVE/consistency/completeness tools. If definitions remain unavailable, run VS2019 MSBuild Debug/Release/solution builds, focused offline actual-code checks of disabled/required/malformed/error paths and existing 003 checks where feasible. Discover existing tests; absent tests are not a pass. No new integration suite, live cloud, baseline, or provisioning. Record tool-only checks as unavailable, not passed.
7. Persist exact commands/results, changed-file list and blockers in `005-summary.md`; update progress. Keep authoritative task status pending and previous metadata intact. Completion commit is allowed only after ALL tasks/checks succeed; blocked execution must not commit.

## Planned files

- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/{Web.config,packages.config,Caldova-Asset-Manager.csproj}`
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Security/{ExternalSecretConfiguration.cs,ConfigurationLoginProviderCredentials.cs}`
- `IaaS2PaaSWeb/Caldova-Asset-Manager.Website/Utils/HttpClientWrapper.cs`
- `docs/KEY-VAULT-SECRETS.md`
- Task-005 plan/progress/summary and focused offline verification source in this report directory.

Do not modify existing task evidence, SQL implementation, migrations, database schema, disabled UI, controllers or deployment scripts. Record any necessary scope refinement before editing.

## Analysis refinement

`ShowRecommendations` is consumed by controllers through `ConfigurationHelpers.GetBool` and by Unity's eagerly constructed HTTP client. To prevent divergent enabled state, change that helper's boolean path to the same external setting reader with secret-safe parse errors; no controller/view changes. Initial build identified Azure.Core1.37/1.38 unification required by the new SDK; add a narrow Azure.Core binding redirect to Web.config while keeping the existing package versions.

Focused credential inventory also confirmed two actual password defaults in the historical source template `IaaS2PaaSWeb/Environments/WorkshopEnv.json` (securestring does not make embedded defaults safe). Remove only those defaults; do not deploy or modernize the template/scripts. To avoid reproducing values in patch output, safely reconstruct the already-read template with those two fields omitted, and verify its canonical semantic SHA256 equals `f9dcb5250ae415415adc4d00296ed2b000a1f924604f94c347dbb5e31b88eb10`. Extend rotation documentation to all affected legacy VM/SQL accounts. Existing generated bin/staging copies are not source; report them as sensitive historical artifacts requiring external cleanup, not proof of credential revocation.
