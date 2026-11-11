# Run from workspace root after a website build. Generated files stay in bin.
$ErrorActionPreference = 'Stop'
$bin = Join-Path (Get-Location) 'IaaS2PaaSWeb\Caldova-Asset-Manager.Website\bin'
Copy-Item IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config "$bin\RecommendationConfigurationCheck.exe.config"
$arguments = @('/nologo', '/target:exe', "/out:$bin\RecommendationConfigurationCheck.exe")
foreach ($reference in @('System.dll', 'System.Core.dll', 'System.Configuration.dll',
    'System.Web.dll', 'System.Net.Http.dll', 'System.Data.dll')) {
    $arguments += "/reference:$reference"
}
foreach ($assembly in @('Caldova-Asset-Manager', 'System.Web.Mvc', 'EntityFramework')) {
    $arguments += "/reference:$bin\$assembly.dll"
}
$arguments += '.github\modernize\plans\report-20261023210809\008-VerifyConfiguration.cs'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$bin\RecommendationConfigurationCheck.exe"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try {
    foreach ($key in @('MachineLearning.ScoringEndpoint', 'MachineLearning.ModelName')) {
        foreach ($variant in @('missing', 'empty')) {
            [xml]$config = Get-Content IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config
            $setting = $config.SelectSingleNode("/configuration/appSettings/add[@key='$key']")
            if ($variant -eq 'missing') { [void]$setting.ParentNode.RemoveChild($setting) }
            else { $setting.SetAttribute('value', '') }
            $config.Save("$bin\RecommendationConfigurationCheck.exe.config")
            Write-Host "File variant: $key $variant"
            & "$bin\RecommendationConfigurationCheck.exe" $key
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }
    }
} finally {
    Copy-Item IaaS2PaaSWeb\Caldova-Asset-Manager.Website\Web.config "$bin\RecommendationConfigurationCheck.exe.config"
}
exit 0
