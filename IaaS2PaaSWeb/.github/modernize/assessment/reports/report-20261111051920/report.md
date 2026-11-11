# $(CaldovaPublishSiteName)

## Summary

| Metric | Value |
|--------|-------|
| Total Issues | 15 |
| Mandatory Blockers | 0 |
| Potential Issues | 11 |

## Component Information

| Property | Value |
|----------|-------|
| Language | C# |
| Frameworks | net10.0 |
| Build tools | MSBuild |

## Cloud Readiness Issues

| Issue Name | Criticality | Story Points | Occurrences |
|------------|-------------|--------------|-------------|
| Data caching is detected | Potential | 3 | [14](#Data_caching_is_detected) |
| Local application configuration detected | Potential | 1 | [4](#Local_application_configuration_detected) |
| SMTP connections detected | Potential | 3 | [4](#SMTP_connections_detected) |
| Access to external resources via HTTP is detected | Potential | 3 | [2](#Access_to_external_resources_via_HTTP_is_detected) |
| Explicit password-based credentials usage is detected | Potential | 3 | [2](#Explicit_password-based_credentials_usage_is_detected) |
| Local or network IO operations detected | Potential | 3 | [1](#Local_or_network_IO_operations_detected) |
| Connection string is detected | Potential | 3 | [1](#Connection_string_is_detected) |
| Hardcoded sensitive data detected | Optional | 3 | [29](#Hardcoded_sensitive_data_detected) |
| Potential duplex communication is detected | Optional | 3 | [3](#Potential_duplex_communication_is_detected) |
| Connection strings without configuration builders detected | Optional | 3 | [2](#Connection_strings_without_configuration_builders_detected) |
| Static content detected | Optional | 3 | [1](#Static_content_detected) |

### Issue Details

<details id="Data_caching_is_detected">
<summary><b>Data caching is detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Utils/LayoutDataAttribute.cs (line 20)`
- `Caldova-Asset-Manager.Website/Utils/LayoutDataAttribute.cs (line 16)`
- `Caldova-Asset-Manager.Website/Controllers/StoreController.cs (line 56)`
- `Caldova-Asset-Manager.Website/Controllers/StoreController.cs (line 51)`
- `Caldova-Asset-Manager.Website/Controllers/HomeController.cs (line 38)`
- `Caldova-Asset-Manager.Website/Controllers/HomeController.cs (line 31)`
- `Caldova-Asset-Manager.Website/Controllers/HomeController.cs (line 34)`
- `Caldova-Asset-Manager.Website/Controllers/HomeController.cs (line 27)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 98)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 132)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 104)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 206)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 178)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 92)`

</details>

<details id="Local_application_configuration_detected">
<summary><b>Local application configuration detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/appsettings.json`
- `Caldova-Asset-Manager.Website/appsettings.json`
- `Caldova-Asset-Manager.Website/appsettings.json`
- `Caldova-Asset-Manager.Website/appsettings.json`

</details>

<details id="SMTP_connections_detected">
<summary><b>SMTP connections detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 13)`
- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 13)`
- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 20)`
- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 20)`

</details>

<details id="Access_to_external_resources_via_HTTP_is_detected">
<summary><b>Access to external resources via HTTP is detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Utils/HttpClientWrapper.cs (line 7)`
- `Caldova-Asset-Manager.Website/Utils/HttpClientWrapper.cs (line 9)`

</details>

<details id="Explicit_password-based_credentials_usage_is_detected">
<summary><b>Explicit password-based credentials usage is detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 19)`
- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 19)`

</details>

<details id="Local_or_network_IO_operations_detected">
<summary><b>Local or network IO operations detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Program.cs (line 56)`

</details>

<details id="Connection_string_is_detected">
<summary><b>Connection string is detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Web.Legacy.config`

</details>

<details id="Hardcoded_sensitive_data_detected">
<summary><b>Hardcoded sensitive data detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Program.cs (line 70)`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Security/AccountMessageSender.cs (line 19)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 39)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 58)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 48)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 35)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 54)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 40)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 59)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 40)`
- `Caldova-Asset-Manager.Website/Models/ManageViewModels.cs (line 59)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 106)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 87)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 102)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 107)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 66)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 83)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 88)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 107)`
- `Caldova-Asset-Manager.Website/Models/AccountViewModels.cs (line 88)`
- `Caldova-Asset-Manager.Website/Controllers/ManageController.cs (line 22)`
- `Caldova-Asset-Manager.Website/Controllers/ManageController.cs (line 23)`
- `Caldova-Asset-Manager.Website/Controllers/AccountController.cs (line 83)`

</details>

<details id="Potential_duplex_communication_is_detected">
<summary><b>Potential duplex communication is detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Hubs/AnnouncementHub.cs (line 4)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 23)`
- `Caldova-Asset-Manager.Website/Areas/Admin/Controllers/StoreManagerController.cs (line 25)`

</details>

<details id="Connection_strings_without_configuration_builders_detected">
<summary><b>Connection strings without configuration builders detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Web.Legacy.config`
- `Caldova-Asset-Manager.Website/Web.Legacy.config`

</details>

<details id="Static_content_detected">
<summary><b>Static content detected</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Caldova-Asset-Manager.csproj`

</details>

## Security Issues

> **Note:** These issues were generated by AI and may contain inaccuracies or incomplete information. Please review carefully.

| Issue Name | Criticality | Story Points | Files |
|------------|-------------|--------------|-------|
| CWE-820: Missing Synchronization | Potential | 8 | [1](#CWE-820_Missing_Synchronization) |
| CWE-821: Incorrect Synchronization | Potential | 8 | [1](#CWE-821_Incorrect_Synchronization) |
| CWE-1057: Data Access Operations Outside of Expected Data Manager Component | Potential | 5 | [1](#CWE-1057_Data_Access_Operations_Outside_of_Expected_Data_Manager_Component) |
| CWE-567: Unsynchronized Access to Shared Data in a Multithreaded Context | Potential | 5 | [1](#CWE-567_Unsynchronized_Access_to_Shared_Data_in_a_Multithreaded_Context) |

### Security Issue Details

<details id="CWE-820_Missing_Synchronization">
<summary><b>CWE-820: Missing Synchronization</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Models/ShoppingCart.cs:92-110`

</details>

<details id="CWE-821_Incorrect_Synchronization">
<summary><b>CWE-821: Incorrect Synchronization</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Models/ShoppingCart.cs:92-110`

</details>

<details id="CWE-1057_Data_Access_Operations_Outside_of_Expected_Data_Manager_Component">
<summary><b>CWE-1057: Data Access Operations Outside of Expected Data Manager Component</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Security/LegacyUserStore.cs`

</details>

<details id="CWE-567_Unsynchronized_Access_to_Shared_Data_in_a_Multithreaded_Context">
<summary><b>CWE-567: Unsynchronized Access to Shared Data in a Multithreaded Context</b> — affected files</summary>

- `Caldova-Asset-Manager.Website/Models/ShoppingCart.cs:92-110`

</details>

---

## Codebase Insights

> **Note:** These documents are generated by AI and may contain inaccuracies or incomplete information. Please review carefully.

1. **[Architecture Diagram](facts/architecture-diagram.md)** — Understand the big picture: system layers and component relationships
2. **[Dependency Map](facts/dependency-map.md)** — Know what the project depends on and where the risks are
3. **[API & Service Contracts](facts/api-service-contracts.md)** — See how services communicate and what contracts they expose
4. **[Data Architecture](facts/data-architecture.md)** — Explore data models, storage, and data flow patterns
5. **[Configuration Inventory](facts/configuration-inventory.md)** — Review how the application is configured across environments
6. **[Business Workflows](facts/business-workflows.md)** — Trace end-to-end business processes and domain logic

[Share feedback](https://aka.ms/ghcp-appmod/feedback)
