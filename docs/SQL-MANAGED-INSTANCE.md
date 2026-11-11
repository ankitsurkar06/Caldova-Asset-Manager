# Azure SQL **Managed Instance** runtime contract

Task 003 targets **Azure SQL Managed Instance (MI), not Azure SQL Database**.
Nothing here provisions resources, deploys the application or moves data.
The current application is still System.Web/OWIN/ASP.NET Identity on **.NET
Framework 4.7.2**, EF6.5.1, Microsoft.EntityFramework.SqlServer6.5.1 and
Microsoft.Data.SqlClient5.2.3. The .NET 10/ASP.NET Core port is blocked (task 001).
Carry this contract forward when that port is implemented; do not treat this
change as .NET 10 acceptance.

## Externally supplied connection

The shared application/Identity connection remains logically named
`DefaultConnectionString`. `SqlDatabaseConfiguration` resolves it in this order:

1. Process environment variable **`SQLCONNSTR_DefaultConnectionString`**.
   A present empty/invalid value fails rather than falling back.
2. `ConfigurationManager.ConnectionStrings["DefaultConnectionString"]`, whose
   provider must be `Microsoft.Data.SqlClient`. An operator/host can inject this
   named value outside source control.

The checked-in named setting is empty. No LocalDB, Windows-auth, SQL-password or
developer-credential fallback is supplied, even in Debug. Host configuration
must reach the worker process before startup; recycle it after changes.
There is no appsettings.json loader on this still-classic runtime. Other host
variable prefixes (for example `SQLAZURECONNSTR_`) are not read by this code;
map the selected host's configuration explicitly to the contract above.

Illustrative **non-secret placeholders**, not an actual endpoint:

```text
Server=tcp:<mi-fqdn>,1433;Initial Catalog=<existing-app-database>;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;
```

- System-assigned identity: omit `User ID`.
- User-assigned identity: append `User ID=<managed-identity-client-id-guid>;`.
  With SqlClient5.2.3 this is the **client/application ID**, not the principal
  (object) ID, resource ID, user name or client secret. The identity must be
  assigned to the application host and available to its managed identity endpoint.
- Use the MI endpoint supplied by its operator. A public MI endpoint typically
  uses port **3342**, not 1433; use its exact public DNS name and port if approved.
- TCP destination and database are mandatory. MI authentication, encryption
  (`True`/Mandatory or Strict) and certificate verification are enforced.
  Passwords, integrated security, attached database files, user instances,
  `Persist Security Info=True` and `TrustServerCertificate=True` are rejected.
- Omitted `Encrypt` currently means Mandatory in SqlClient5.2.3; specify it
  explicitly for a stable operational contract. Do not bypass TLS to fix DNS or
  certificate errors. Use the endpoint's certificate-matching DNS name.
- Startup validates configuration **without** acquiring a token or opening SQL.
  Missing/insecure/malformed settings fail with a fixed message, never the
  supplied connection string. Valid syntax does not prove the target is MI:
  operators must verify the actual resource type, endpoint and database.
- Tokens are acquired/refreshed by SqlClient's managed-identity authentication;
  no access token/password is persisted in this repository or browser output.
  A developer workstation without a managed identity cannot run real SQL access
  using this production contract. Offline checks do not need one.

## Runtime never installs or seeds the database

`CaldovaAssetManagerContext` disables its EF initializer in a static constructor,
including non-HTTP usage. Startup no longer registers the legacy sample
initializer. OWIN no longer creates an administrator, role or claim at startup.
The old sample initializer source is retained for historical compatibility,
but is **not registered**; do not wire it into production. No runtime automatic
migration is configured. Missing database/schema/permissions fail visibly rather
than being repaired by the web process.

Ordinary application writes (orders, carts, account registration, password
changes, etc.) remain enabled under the same application authorization. Managed
identity here authenticates the **application to SQL**, not end users. Existing
cookies, external login configuration and ASP.NET Identity password formats are
not replaced by Entra user sign-in.

The old administrator configuration is no longer consumed by startup. Removal
and rotation of previously embedded credentials remain task 005; do not reuse
them. Establish any genuinely new administrator through a separately approved,
audited process, not source-controlled bootstrap passwords or startup code.
Preserve migrated administrator IDs, roles and claims.

## External identity and SQL permissions (operator/DBA responsibility)

Before runtime acceptance:

1. Supply an MI in the intended tenant and network, an Entra administrator and
   a managed identity on the eventual application host. No host has been chosen
   or deployed by this task.
2. Using an authorized external administration identity, establish the intended
   MI Entra login/database-user mapping for the runtime managed identity
   (MI supports Entra server principals; contained database users may also be
   appropriate). Resolve the exact principal, tenant and database; do not rely
   on ambiguous display names. If directory lookup requires permissions for the
   MI service identity, have the tenant administrator grant only the documented
   directory permissions appropriate to that mapping.
3. Grant connection and only the table/view read/write rights the application's
   storefront, carts, checkout, administration and Identity stores need.
   Prefer a dedicated database role scoped to the existing application objects.
   Assess whether schema-level SELECT/INSERT/UPDATE/DELETE is justified; do not
   blindly grant broad rights. EXECUTE is needed only for actual stored
   procedures/functions in the migrated workload. Identity account/role/claim
   writes require their relevant table rights.
4. Do **not** make the web identity sysadmin, db_owner, db_ddladmin, a schema
   deployer, or grant CREATE DATABASE/ALTER schema to enable startup. Azure
   resource RBAC alone does not create a SQL user or grant SQL data access.
5. Use a separate, temporary/audited deployment identity for schema/data work,
   with only the elevated permissions required by that procedure. Revoke those
   rights after migration; the runtime identity must not inherit them.

No SQL permission script is auto-executed. The precise grants depend on the
externally migrated objects and chosen principal mapping.

## Controlled external schema and data migration

No target database was specified and no data movement has been performed.
The DBA must plan a supported SQL Server-to-**MI** backup/restore, migration
service or other approved migration, including compatibility assessment:

1. Inventory the authoritative source schema/data, SQL/EF migration history,
   SQL version/compatibility level, collation, constraints, indexes, triggers,
   identities/sequences, logins/users, jobs and cross-database dependencies.
   The repository SQL project and historical EF migrations are not proof of the
   live production schema. Review generated DDL before use; never deploy the
   sample initializer or an empty-database initializer over production data.
2. Take tested backups; rehearse restore and rollback. Schedule a write freeze
   or supported synchronization/cutover process so late orders/account/cart
   writes are not lost. Retain a recoverable source until acceptance.
3. Preserve every application key/foreign key, order/detail, inventory, cart,
   store/raincheck and other records. Preserve **AspNetUsers IDs, password
   hashes, security stamps, user logins, roles, memberships and claims**.
   Do not recreate or reseed users, reset hashes, renumber keys or change column
   semantics. Retain authentication data-protection/machine-key continuity
   externally where existing cookies/tokens must survive a host move.
4. Keep SQL Server semantics: collation/case behavior, decimals, dates, nulls,
   identity generation, transaction/isolation behavior, constraints and EF
   mappings. Select MI compatibility level deliberately; do not introduce new
   transient retries around non-idempotent transactions as part of this change.
5. Compare row counts, keys/relationships and critical business totals; verify
   query plans and functional storefront/admin/Identity flows in the externally
   approved acceptance environment. Validate DML transactions and existing
   users' password/external-login access; do not infer success from a build.
6. Apply any reviewed schema delta using the **deployment** identity, map
   runtime permissions, then supply the MI connection through host configuration.
   On failure use the documented rollback/write-reconciliation procedure.

This task does not change model mappings, EF migration files, LINQ/SQL queries
or transaction boundaries. That preserves code semantics, but does not prove
that an externally migrated database is equivalent.

## MI networking, DNS and encrypted transport

- MI normally sits in a VNet. The host needs an approved route to that network
  (for example network integration/peering/VPN as appropriate), correctly
  configured DNS resolution for the exact MI FQDN and permitted outbound and
  inbound network rules. No such integration is provisioned here.
- Have the MI/network operator confirm endpoint type and connection policy.
  VNet-local proxy commonly uses TCP 1433; redirect can additionally require
  TCP 11000-11999 to the relevant MI network destinations. Public endpoints
  use TCP 3342. Private endpoints and newer MI connection policies can have
  different requirements: allow only the documented ports/routes for the
  actual selected configuration, not a blanket internet rule.
- Ensure access to the host's managed-identity token endpoint and necessary
  identity services. Managed identity does not bypass MI networking/firewalls.
- Use TLS 1.2+ supported by the platform, a trusted server certificate chain and
  the MI FQDN (not a raw IP or unvalidated alias). Keep certificate validation on.
  Configure OS trust and DNS correctly instead of disabling encryption.

## Evidence and remaining acceptance

Local evidence and exact commands are in
[`003-summary.md`](../.github/modernize/plans/report-20261023210809/003-summary.md).
Offline checks validate the actual compiled context/provider, configuration
rejection, generated SQL and initializer policy without connecting to SQL.
They do not validate tokens, MI permissions, DNS, TLS handshakes, data migration
or transaction execution. There is no existing unit-test project.
The .NET 10 dependency and externally operated Azure acceptance remain open.
