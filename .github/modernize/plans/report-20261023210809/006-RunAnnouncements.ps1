# Run from workspace root after rebuilding the website; generated artifacts stay in bin.
$ErrorActionPreference = 'Stop'
$bin = Join-Path (Get-Location) 'IaaS2PaaSWeb\Caldova-Asset-Manager.Website\bin'
Copy-Item IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config "$bin\AnnouncementCheck.exe.config"
$references = @(
    'System.Web.dll', 'System.Core.dll', 'System.Data.dll', 'System.Configuration.dll',
    'System.Runtime.Caching.dll', 'Microsoft.CSharp.dll', 'System.ComponentModel.DataAnnotations.dll',
    'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.Runtime.dll'
)
$assemblies = @(
    'Caldova-Asset-Manager', 'EntityFramework', 'Microsoft.AspNet.Identity.Core',
    'Microsoft.AspNet.Identity.EntityFramework', 'Microsoft.AspNet.SignalR.Core',
    'Microsoft.Owin', 'Owin', 'Microsoft.Practices.Unity', 'Newtonsoft.Json',
    'System.Web.Mvc', 'System.Web.Http', 'System.Web.Http.WebHost'
)
$arguments = @('/nologo', '/target:exe', "/out:$bin\AnnouncementCheck.exe")
foreach ($reference in $references) { $arguments += "/reference:$reference" }
foreach ($assembly in $assemblies) { $arguments += "/reference:$bin\$assembly.dll" }
$arguments += '.github\modernize\plans\report-20261023210809\006-VerifyAnnouncements.cs'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$bin\AnnouncementCheck.exe"
exit $LASTEXITCODE
