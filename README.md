# Caldova-Asset-Manager

## Fresh assessment recording baseline

Branch: `assessment/caldova-recording`.

This branch changes the application identity to **Caldova-Asset-Manager** while
preserving the original .NET Framework 4.5.1 application for a before-modernization
assessment. It does not include the separate .NET 10 port, SQL MI migration,
generated assessment reports or machine-local assessment state.

On the recording machine:

```powershell
git clone --branch assessment/caldova-recording --single-branch https://github.com/ankitsurkar06/Caldova-Asset-Manager.git
Set-Location .\Caldova-Asset-Manager
code .
```

Open [Caldova-Asset-Manager.sln](IaaS2PaaSWeb/Caldova-Asset-Manager.sln).
The website project is
[Caldova-Asset-Manager.csproj](IaaS2PaaSWeb/PartsUnlimitedWebsite/Caldova-Asset-Manager.csproj).
The project display name, assembly title and product now identify Caldova.
Existing source namespaces, database names and workshop Azure resource names
remain unchanged.

Install the GitHub Copilot modernization extension and its assessment prerequisites.
This legacy ASP.NET project requires Windows MSBuild/Visual Studio ASP.NET web
application build targets and the .NET Framework 4.5.1 reference assemblies;
the modern .NET SDK alone is insufficient. Restore NuGet packages with the
repository's NuGet executable after installing the required tooling.
Run a **new** assessment on this checkout; do not reuse a previous report.
The rename does not guarantee assessment success if prerequisites are missing.


<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fkarlrissland%2FAppWorkshop%2Fmaster%2FIaaS2PaaSWeb%2FEnvironments%2FWorkshopEnv.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

This allows you to deploy the workshop environment.  When complete, you should have a Web Server and SQL server fully configured in their own vNet and Resource group.

## Prerequisites

There are no prerequistes for the deployment.

## Deployment steps

Click the "deploy to Azure" button at the beginning of this document.

Enter vm admin username and password

You can leave default values for the other parameters

NOTE: do not change the value of _artifactsLocation or _artifactsLocationSas as the deployment will fail.
