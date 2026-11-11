# Task 005 — implemented work and verification evidence

## Result

Implemented independent feasible credential removal and managed-identity Key Vault
access on **net472 / MVC5 / OWIN / ASP.NET Identity / EF6.5.1**, retaining
Microsoft.Data.SqlClient5.2.3 and task003's MI configuration/no-startup-seeding
behavior. **Not** a .NET10 port and **not** full task acceptance: dependencies
001/004 and callable mandatory MCP checks remain blocked. Confirmed plan and
`.metadata/tasks.json` were not edited; task005 stays pending. No schema repair.

- Removed administrator bootstrap username/password settings from Web.config.
- Removed two actual password defaults from historical
  `Environments/WorkshopEnv.json`; the securestring parameters now have no
  defaults. The entire template's canonical JSON hash matches the pre-edit
  fingerprint after omitting only those two fields:
  `f9dcb5250ae415415adc4d00296ed2b000a1f924604f94c347dbb5e31b88eb10`.
  Safe reconstruction avoided reproducing password values in the template patch.
- Replaced provider/API plaintext configuration consumption with vault secret
  **name references**. Public provider client IDs remain external nonsecret settings.
- Added Azure.Security.KeyVault.Secrets4.6.0, using existing
  Azure.Core1.38.0/Azure.Identity1.11.4; no broad package upgrade.
- SDK SecretClient authenticates exclusively with ManagedIdentityCredential,
  optional user-assigned client GUID. No client secret, developer login chain,
  inline secret or SQL-password fallback. Azure vault HTTPS root/name validation,
  bounded secret request, logging disabled, explicit disabled/expired/future secret
  rejection and secret-safe exceptions without raw SDK inner exceptions.
- Explicit provider enabled flags default false. Disabled social providers and
  recommendations perform no vault lookup. Enabled features fail visibly when
  required configuration/secrets are missing. Shared host-aware boolean reader
  keeps recommendation UI/controller and singleton HTTP client aligned.
- Cookie/Identity/security-stamp/roles/claims/provider middleware are unchanged;
  no Entra end-user sign-in migration or authorization edits. Existing API Basic
  header contract retained. The historical recommendation endpoint and its
  HTTP failure behavior were not modernized; recommendations stay disabled.
- Added operator instructions for external identity/RBAC/network setup, versionless
  rotation/restart, issuer/account revocation, sensitive generated artifacts and
  controlled one-time administrator bootstrap/recovery.

## Manually added/changed source files

Paths relative to `C:\Users\ankitsurkar\Caldova-Asset-Manager`:

1. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Security\ExternalSecretConfiguration.cs` — new.
2. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Security\ConfigurationLoginProviderCredentials.cs`.
3. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Utils\HttpClientWrapper.cs`.
4. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Utils\ConfigurationHelpers.cs` — boolean path only.
5. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config`.
6. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\packages.config` — one package declaration.
7. `IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj` — one SDK reference and one Compile item.
8. `IaaS2PaaSWeb\Environments\WorkshopEnv.json` — two password defaults only, semantically verified.
9. `docs\KEY-VAULT-SECRETS.md` — new.
10. This report folder: `005-plan.md`, `005-progress.md`, `005-summary.md`,
    `005-VerifySecrets.cs`, `005-VerifyStructure.py` — new task evidence.

Builds/restore also produce normal bin/obj/package-cache outputs and temporary
offline check executables/config. They are not source implementation files.
Restore acquired Azure.Core1.37.0 and System.Text.Encodings.Web4.7.2 package
folders as lowest dependencies but did **not** downgrade the project references
or declarations (Core1.38.0/Encodings6.0.0 retained).

No source edits to controllers, Identity/sign-in implementation, routes, views,
SQL configuration/context, migrations, database schema, existing task evidence,
confirmed plan/metadata, project renames, README or pre-existing docs/scripts.
Existing unrelated dirty changes were not reset or overwritten. Exact whole-tree
dirty status cannot be attested: permitted version-control tool definitions were
unavailable. No direct git commands were used. Supplied branch remains the
execution branch `modernize/dotnet-20261023211948`; no switching, stash or commit.

## Tool availability and scope blockers

Discovery of appmod-run-task returned tool names only, not callable definitions.
No invocation/context/session ID could be produced. Other searches similarly
returned names without callable schemas for version-control, build, tests,
consistency/completeness/CVE and .NET dependency guidance. First broad search
also failed with an embeddings500; specific retries returned names, not definitions.
Preview tool not callable; local files read through view instead.

Consequently no MCP status inspection, planning context, KB guidance retrieval,
build/test/CVE/consistency/completeness tool pass or final commit is claimed.
Local available tooling was used for actual implementation and verification,
not as a claim those missing tool checks succeeded. No existing unit-test project
was found; focused offline checks below are not a newly added integration suite.
No CVE scan/remediation was performed. Overall blocked execution did not satisfy
the all-success precondition for committing.

## Exact local commands and results

Commands ran from workspace root. No secrets were passed as arguments or printed
in evidence. The synthetic marker in the offline check is not a credential.

### Restore and build

Initial restore attempt using `-IgnoreDependencies` failed exit1 because the
repository's nuget.exe does not support that option; retried successfully:

```powershell
& .\IaaS2PaaSWeb\nuget.exe install Azure.Security.KeyVault.Secrets -Version 4.6.0 -OutputDirectory IaaS2PaaSWeb\packages -Source https://www.nuget.org/api/v2/ -NonInteractive -Framework net472
```

Exit0. Package nuspec confirms Azure.Core minimum1.37.0, met by existing1.38.0.
The first Debug build succeeded with MSB3247 Azure.Core version unification
warning. A targeted Web.config1.38.0 binding redirect fixed subsequent website
builds without changing existing dependency versions.

Final builds:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Release /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Caldova-Asset-Manager.csproj /t:Rebuild /p:Configuration=Debug /nologo /verbosity:minimal
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe' IaaS2PaaSWeb\Caldova-Asset-Manager.sln /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
```

All exit0. Release emitted the existing `ManageController.cs(135,41)` CS1998
async-without-await warning. Final Debug/solution output had no warnings.
No SQL/database deployment was invoked.

### Actual compiled secret-path checks

Ran after final Release and again after final Debug/solution builds:

```powershell
$bin=Join-Path (Get-Location) 'IaaS2PaaSWeb\Caldova-Asset-Manager.Website\bin'
Copy-Item IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config "$bin\SecretConfigurationCheck.exe.config"
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$bin\SecretConfigurationCheck.exe" /reference:System.Configuration.dll /reference:System.Net.Http.dll /reference:'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\netstandard.dll' "/reference:$bin\Azure.Core.dll" "/reference:$bin\Azure.Identity.dll" "/reference:$bin\Azure.Security.KeyVault.Secrets.dll" "/reference:$bin\Caldova-Asset-Manager.dll" .github\modernize\plans\report-20261023210809\005-VerifySecrets.cs
if ($LASTEXITCODE -eq 0) { & "$bin\SecretConfigurationCheck.exe"; exit $LASTEXITCODE } else { exit $LASTEXITCODE }
```

Both exit0, **45 assertions each**. The standalone legacy csc emits CS1701
(SDK compiled against Core1.37 versus referenced1.38); copied actual Web.config
supplies the redirect and real SDK/ManagedIdentityCredential construction passes.
No token acquisition or network call occurs.

Coverage: actual configuration has no bootstrap/inline secrets; providers/API
disabled without vault access; shared host switch; malformed switches sanitized;
enabled missing client/ref failures; system/user-assigned lookup inputs;
unsupported vault URLs/ports/paths/query/userinfo/hosts and invalid names/IDs
rejected; public/China/Gov suffixes; raw failing fetch exceptions and empty/null
values sanitized; disabled/expired/future/null vault objects rejected; real SDK
assembly loading. Successful secret lookup/error responses are supplied through
reflection to the private resolver using in-memory delegates, **not** live Key
Vault, OWIN sign-in or issuer verification.

### Prior SQL behavior and structural checks

```powershell
$bin=Join-Path (Get-Location) 'IaaS2PaaSWeb\Caldova-Asset-Manager.Website\bin'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe "/out:$bin\ManagedInstanceCheck.exe" /reference:System.Data.dll /reference:System.Configuration.dll /reference:System.Xml.Linq.dll /reference:System.ComponentModel.DataAnnotations.dll "/reference:$bin\EntityFramework.dll" "/reference:$bin\Microsoft.EntityFramework.SqlServer.dll" "/reference:$bin\Microsoft.Data.SqlClient.dll" "/reference:$bin\Microsoft.AspNet.Identity.EntityFramework.dll" "/reference:$bin\Microsoft.AspNet.Identity.Core.dll" "/reference:$bin\Caldova-Asset-Manager.dll" .github\modernize\plans\report-20261023210809\003-VerifyManagedInstance.cs
if ($LASTEXITCODE -eq 0) { & "$bin\ManagedInstanceCheck.exe" IaaS2PaaSWeb\Caldova-Asset-Manager.Website; exit $LASTEXITCODE } else { exit $LASTEXITCODE }
python .github\modernize\plans\report-20261023210809\003-VerifyStructure.py
python .github\modernize\plans\report-20261023210809\005-VerifyStructure.py
```

All exit0. All **44 existing MI assertions** still pass: shared provider/context,
managed-identity/encryption policy, no bootstrap/initializer/database opens,
query/Identity mappings, named/environment precedence and secret-safe errors.
Both structural scripts pass. All81 declared package folders and assembly
HintPaths exist; XML parses; original dependency graph/pending task statuses
and known-bug metadata schema remain intact; canonical ARM-template fingerprint
and operator documentation checks pass.

The existing 003 structural script prints "no task-003 dependency additions";
that refers to task003, not this task's one new SDK package. It also prints
generic local consistency/completeness success; this is not an MCP pass.

## Remaining external work / no cloud-success claim

- Full .NET10/ASP.NET Core port and task004 remain blocked; ordering/dependency
  acceptance has not been waived.
- No live managed identity, Key Vault, RBAC/network/private DNS, external provider
  sign-in, recommendation endpoint, database connectivity or SQL execution tested.
- No rotation/revocation, bootstrap, account/session change, provisioning,
  deployment, containerization, baseline or integration suite executed.
- Historical `Environments/bin/Debug`/staging artifacts were observed to contain
  old template defaults; existing unrelated generated files were not purged.
  Externally quarantine/clean those artifacts and old deployed packages and rotate
  affected application-admin/VM/SQL/provider credentials. Source deletion alone
  does not revoke credentials or remove repository history.
- Configure the intended managed identity, vault contents, minimal permissions
  and nonsecret references externally, keep disabled features disabled until
  deliberately validated, and recycle all instances after versionless rotation.
