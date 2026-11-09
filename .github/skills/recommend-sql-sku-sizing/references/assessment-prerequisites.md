# Existing local tool prerequisites

The skill-led readiness caller performs prerequisite checks with short local commands.
Require Windows, PowerShell 7, Azure CLI >= 2.75.0, datamigration and a prepared
SQL assessment executable supporting configuration-based sizing, combined
saved-report generation and the new SKU report metadata.

Do not repeat the caller's checks or use fragile JMESPath quoting.
The caller checks the download-center package identity and local archive hash
before reusing its download cache; otherwise it refreshes the native package.
The installed extension alone does not ensure freshness. The caller can pin an
explicit prepared build through `SqlAssessmentPath`. Do not repeat downloads or
install, upgrade or sign in independently in this supporting workflow.

Configuration-based sizing needs the assessed connection. Performance-based
sizing uses completed CSV data and needs no SQL connection or Azure login.
No new performance-data collection is supported.

Propagate the caller's safe prerequisite diagnostic without raw tool exceptions.
