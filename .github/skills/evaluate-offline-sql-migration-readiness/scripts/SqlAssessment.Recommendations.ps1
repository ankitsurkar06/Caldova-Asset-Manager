function Invoke-SqlRecommendationCommand {
    param([string[]]$Arguments, [string]$CommandStage)

    $remaining = [int]($ASSESSMENT_DEADLINE_UTC - [DateTime]::UtcNow).TotalMilliseconds
    if ($remaining -le 0) {
        throw "[evaluate-offline-sql-migration-readiness] EXECUTION_TIMEOUT: Stage ${CommandStage}: the shared execution budget is exhausted."
    }
    $response = Invoke-SqlTool $SQL_ASSESSMENT_EXECUTABLE $Arguments $CommandStage $remaining
    $failure = Get-SqlFailureCategory ($response.Output + "`n" + $response.Error)
    if ($failure) {
        throw "[evaluate-offline-sql-migration-readiness] $($failure.Code): Stage ${CommandStage}: native exit code $($response.ExitCode). $($failure.Reason)"
    }
    if ($response.ExitCode -ne 0) {
        throw "[evaluate-offline-sql-migration-readiness] ASSESSMENT_FAILED: Stage ${CommandStage}: native exit code $($response.ExitCode). Verify the prepared native package and inputs outside assessment; raw diagnostics were withheld."
    }
}

function Copy-SqlPerformanceInputs {
    param([string]$Destination, [string]$ReportInstance)

    if (-not [IO.Directory]::Exists($PerformanceDataPath)) {
        throw '[evaluate-offline-sql-migration-readiness] PERFORMANCE_DATA_REQUIRED: Supply an existing absolute performanceDataPath with completed instance-specific aggregated counters. No configuration-based fallback was attempted.'
    }
    $files = @(Get-ChildItem -LiteralPath $PerformanceDataPath -File -ErrorAction Stop)
    $prefix = $ReportInstance.Replace('\', '_')
    $counter = @($files | Where-Object { $_.Name -ieq ($prefix + '_PerformanceAggregated_Counters.csv') })
    if ($counter.Count -ne 1) {
        throw '[evaluate-offline-sql-migration-readiness] PERFORMANCE_DATA_REQUIRED: performanceDataPath must contain nonempty completed aggregated counters for the assessed canonical instance. No collection or fallback was attempted.'
    }
    if ($counter[0].Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw '[evaluate-offline-sql-migration-readiness] PERFORMANCE_DATA_INVALID: Performance inputs must be regular files, not filesystem links.'
    }
    if (-not ($counter[0] | Import-Csv -ErrorAction Stop | Select-Object -First 1)) {
        throw '[evaluate-offline-sql-migration-readiness] PERFORMANCE_DATA_REQUIRED: performanceDataPath must contain nonempty completed aggregated counters for the assessed canonical instance. No collection or fallback was attempted.'
    }
    foreach ($file in $files | Where-Object {
        $_.Extension -ieq '.csv' -and (
            $_.Name.StartsWith($prefix + '_', [StringComparison]::OrdinalIgnoreCase) -or
            $_.Name.StartsWith('Common_', [StringComparison]::OrdinalIgnoreCase))
    }) {
        if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw '[evaluate-offline-sql-migration-readiness] PERFORMANCE_DATA_INVALID: Performance inputs must be regular files, not filesystem links.'
        }
        [IO.File]::Copy($file.FullName, (Join-Path $Destination $file.Name), $false)
    }
}

function Invoke-SqlRecommendations {
    param($Suitability, [string]$PrivateConfigPath)

    $instance = [string]$Suitability.Report.Servers[0].Properties.ServerName
    $safeInstance = $instance.Replace('\', '_')
    foreach ($character in [IO.Path]::GetInvalidFileNameChars()) { $safeInstance = $safeInstance.Replace([string]$character, '') }
    $criterion = if ($PerformanceDataPath) { 'PerformanceBased' } else { 'ConfigBased' }
    $directory = Join-Path $OUTPUT_FOLDER ('combined\' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($directory)
    $suitabilityName = [IO.Path]::GetFileName($Suitability.JsonPath)
    if ($suitabilityName -notmatch '^SqlAssessment-.+-\d{12}(?:\d{2})?\.json$') {
        throw '[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage combined-inputs: suitability must use the native timestamped saved-report format. Prepare a package containing both features and explicitly request fresh results for legacy reports.'
    }
    [IO.File]::Copy($Suitability.JsonPath, (Join-Path $directory $suitabilityName), $false)
    if ($PerformanceDataPath) { Copy-SqlPerformanceInputs $directory $instance }
    $skuPaths = [Collections.Generic.List[string]]::new()
    $targets = [ordered]@{
        DB = 'AzureSqlDatabase'
        MI = 'AzureSqlManagedInstance'
        VM = 'AzureSqlVirtualMachine'
    }
    foreach ($target in $targets.GetEnumerator()) {
        $configuration = @{
            action = 'GetSkuRecommendation'
            sizingCriterion = $criterion
            targetPlatform = $target.Value
            outputFolder = $directory
            overwrite = $false
        }
        if ($PerformanceDataPath) {
            $configuration.targetSqlInstance = $instance
        } else {
            if (-not $PrivateConfigPath) {
                throw '[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: Configuration-based sizing requires the assessed protected connection context.'
            }
            $privateConfiguration = [IO.File]::ReadAllText($PrivateConfigPath) | ConvertFrom-Json -ErrorAction Stop
            $configuration.sqlConnectionStrings = @($privateConfiguration.sqlConnectionStrings)
        }
        try {
            if ($PerformanceDataPath) {
                $arguments = @('GetSkuRecommendation', '--sizingCriterion', $criterion,
                    '--targetPlatform', $target.Value, '--targetSqlInstance', $instance,
                    '--outputFolder', $directory, '--overwrite', 'false')
            } else {
                [IO.File]::WriteAllText($PrivateConfigPath, ($configuration | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
                $arguments = @('LoadFromJson', '--configFile', $PrivateConfigPath)
            }
            $generatedAfter = [DateTimeOffset]::UtcNow
            Invoke-SqlRecommendationCommand $arguments ('sku-' + $target.Key)
        } finally {
            $configuration = $null
            $privateConfiguration = $null
        }
        $pattern = '^SkuRecommendationReport-' + [regex]::Escape($safeInstance) + '-' + $target.Key + '-\d{14}\.json$'
        $candidates = @(Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Name -match $pattern })
        if ($candidates.Count -ne 1) {
            throw '[evaluate-offline-sql-migration-readiness] RECOMMENDATION_FAILED: Stage sku-validation: each requested target must produce one new instance-specific SKU report. Exit zero is insufficient.'
        }
        try {
            $report = [IO.File]::ReadAllText($candidates[0].FullName) | ConvertFrom-Json -ErrorAction Stop
        } catch {
            throw '[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage sku-validation: native SKU JSON could not be read or parsed. Raw report contents were withheld.'
        }
        $servers = @($report.Servers)
        $created = [DateTimeOffset]::MinValue
        $validTime = if ($report.TimeCreated -is [DateTime]) {
            $created = [DateTimeOffset]$report.TimeCreated
            $true
        } else {
            [DateTimeOffset]::TryParse([string]$report.TimeCreated, [ref]$created)
        }
        if (-not $validTime -or $created -lt $generatedAfter -or
            $servers.Count -ne 1 -or $servers[0].ServerName -ine $instance -or
            $servers[0].TargetPlatform -ine $target.Value -or
            $null -eq $servers[0].Recommendations -or $null -eq $servers[0].Requirements.DatabaseLevelRequirements) {
            throw '[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage sku-validation: missing or mismatched native source, target, time, requirements or recommendation metadata.'
        }
        $expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($database in $Suitability.Report.Servers[0].Databases) {
            [void]$expected.Add([string]$database.Properties.Name)
        }
        $actual = @($servers[0].Requirements.DatabaseLevelRequirements | ForEach-Object { [string]$_.DatabaseName })
        if ($actual.Count -ne $expected.Count -or -not $expected.SetEquals([string[]]$actual)) {
            throw '[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage sku-validation: SKU database identities must exactly match suitability; incomplete or duplicate coverage cannot be published.'
        }
        $skuPaths.Add($candidates[0].FullName)
    }
    Invoke-SqlRecommendationCommand @('GenerateAssessmentReport', '--reportFolder', $directory, '--serverInstance', $instance) 'combined-report'
    $pattern = '^AssessmentReport-' + [regex]::Escape($safeInstance) + '-\d{14}\.html$'
    $html = @(Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Name -match $pattern })
    if ($html.Count -ne 1 -or $html[0].Length -eq 0) {
        throw '[evaluate-offline-sql-migration-readiness] REPORT_DATA_UNAVAILABLE: Stage combined-report: one new nonempty combined native HTML report is required. No readiness-only fallback was published.'
    }
    & (Join-Path $PSScriptRoot 'Publish-SqlAssessmentBatch.ps1') -OutputDirectory $OUTPUT_FOLDER -NativeHtmlPaths $html[0].FullName -ValidateOnly
    [PSCustomObject]@{
        JsonPath = $Suitability.JsonPath
        Report = $Suitability.Report
        GeneratedAtUtc = $Suitability.GeneratedAtUtc
        HtmlPath = $html[0].FullName
        SuitabilityHtmlPath = $Suitability.HtmlPath
        SkuJsonPaths = $skuPaths.ToArray()
        SizingCriterion = $criterion
    }
}
