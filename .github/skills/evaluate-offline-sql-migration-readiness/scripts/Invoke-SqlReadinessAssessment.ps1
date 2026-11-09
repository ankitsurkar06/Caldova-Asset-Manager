[CmdletBinding()]
param(
    [string]$AssessmentOutputDirectory,
    [string]$InstanceName,
    [string]$AuthMode,
    [string]$ConfigFilePath,
    [string]$SqlUserName,
    [string]$KeyVaultName,
    [string]$SecretName,
    [string]$CredentialTarget,
    [string]$TrustServerCertificate = 'true',
    [string]$ReportFormat = 'html',
    [string]$PerformanceDataPath,
    [string]$SqlAssessmentPath,
    [switch]$Fresh,
    [datetime]$DeadlineUtc = [DateTime]::UtcNow.AddMinutes(7)
)

$ErrorActionPreference = 'Stop'
$prefix = '[evaluate-offline-sql-migration-readiness] '
$stage = 'input-validation'
function Get-SafeSqlDiagnostic($ErrorRecord) {
    if ($ErrorRecord.Exception.Message.StartsWith($prefix, [StringComparison]::Ordinal)) {
        return $ErrorRecord.Exception.Message
    }
    return "${prefix}ASSESSMENT_FAILED: Stage ${stage}: local execution failed; raw errors were withheld. Verify local tooling and file access."
}

try {
    if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -lt 7) {
        throw "${prefix}UNSUPPORTED_HOST: Windows and PowerShell 7 are required."
    }
    if ([string]::IsNullOrWhiteSpace($AssessmentOutputDirectory) -or -not [IO.Path]::IsPathFullyQualified($AssessmentOutputDirectory) -or $AssessmentOutputDirectory -match '["%\r\n]') {
        throw "${prefix}INVALID_PARAMETERS: AssessmentOutputDirectory must be an absolute Windows path without shell expansion or control characters."
    }
    if ([string]::IsNullOrWhiteSpace($InstanceName)) {
        throw "${prefix}MISSING_PARAMETERS: Supply one resolved InstanceName. Discover application endpoints before per-instance execution."
    }
    if ($InstanceName -and ($InstanceName -notmatch '^(?:tcp:)?(?:[a-zA-Z0-9_.-]+|\[[0-9a-fA-F:]+\]|\(local(?:db)?\))(?:\\[a-zA-Z0-9_$.-]+)?(?:,\d{1,5})?$')) {
        throw "${prefix}INVALID_PARAMETERS: InstanceName must be one literal SQL endpoint, not a connection string or expression."
    }
    if ($AuthMode -and $AuthMode -notin @('Windows', 'EntraDefault', 'ConfigFile', 'KeyVault', 'CredentialManager')) {
        throw "${prefix}INVALID_PARAMETERS: AuthMode must be Windows, EntraDefault, ConfigFile, KeyVault or CredentialManager."
    }
    if ($ConfigFilePath) {
        if (-not [IO.Path]::IsPathFullyQualified($ConfigFilePath)) {
            throw "${prefix}INVALID_PARAMETERS: ConfigFilePath must be an absolute Windows path to protected JSON, not its contents."
        }
        if ($AuthMode -and $AuthMode -ne 'ConfigFile') {
            throw "${prefix}CONFLICTING_PARAMETERS: ConfigFilePath conflicts with a non-ConfigFile AuthMode."
        }
        $AuthMode = 'ConfigFile'
    }
    $required = switch ($AuthMode) {
        'ConfigFile' { @{ ConfigFilePath = $ConfigFilePath } }
        'KeyVault' { @{ SqlUserName = $SqlUserName; KeyVaultName = $KeyVaultName; SecretName = $SecretName } }
        'CredentialManager' { @{ CredentialTarget = $CredentialTarget } }
    }
    if ($required) {
        $missing = @($required.Keys | Where-Object { [string]::IsNullOrWhiteSpace($required[$_]) } | Sort-Object)
        if ($missing.Count) { throw "${prefix}MISSING_PARAMETERS: Supply non-secret selectors: $($missing -join ', '). Never supply credentials in the prompt." }
    }
    if ($ReportFormat -notin @('html', 'markdown') -or $TrustServerCertificate -notin @('true', 'false')) {
        throw "${prefix}INVALID_PARAMETERS: ReportFormat must be html or markdown; TrustServerCertificate must be true or false."
    }
    foreach ($path in @($PerformanceDataPath, $SqlAssessmentPath) | Where-Object { $_ }) {
        if (-not [IO.Path]::IsPathFullyQualified($path) -or $path -match '["%\r\n]') {
            throw "${prefix}INVALID_PARAMETERS: PerformanceDataPath and SqlAssessmentPath must be absolute Windows paths without shell expansion or control characters."
        }
    }
    if ($PerformanceDataPath -and -not [IO.Directory]::Exists($PerformanceDataPath)) {
        throw "${prefix}PERFORMANCE_DATA_REQUIRED: Supply an existing performanceDataPath. Invalid supplied data never selects configuration-based sizing."
    }
    $ASSESSMENT_DEADLINE_UTC = $DeadlineUtc.ToUniversalTime()
    . (Join-Path $PSScriptRoot 'SqlAssessment.Process.ps1')
    . (Join-Path $PSScriptRoot 'SqlReadiness.Helpers.ps1')
    . (Join-Path $PSScriptRoot 'SqlAssessment.Recommendations.ps1')
    if (-not $SqlAssessmentPath -or -not [IO.File]::Exists($SqlAssessmentPath)) {
        throw "${prefix}PREREQUISITE_REQUIRED: Stage native-package: SqlAssessmentPath must identify the prepared executable selected by the skill."
    }
    $SQL_ASSESSMENT_EXECUTABLE = $SqlAssessmentPath
    if ($AuthMode -eq 'KeyVault') {
        $AZ_EXECUTABLE = (Get-Command az -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    }
    $TRUST_SERVER_CERTIFICATE = $TrustServerCertificate -eq 'true'
    $SQL_USER_NAME = $SqlUserName
    $KEY_VAULT_NAME = $KeyVaultName
    $SECRET_NAME = $SecretName
    $CREDENTIAL_TARGET = $CredentialTarget
    $INSTANCE = $InstanceName
    $AUTH_MODE = $AuthMode
    $USER_CONFIG_PATH = $ConfigFilePath
    $OUTPUT_FOLDER = [IO.Path]::GetFullPath($AssessmentOutputDirectory)
    $stage = 'target-execution'
    if ([DateTime]::UtcNow -ge $ASSESSMENT_DEADLINE_UTC) {
        throw "${prefix}EXECUTION_TIMEOUT: Stage target-execution: the shared execution budget is exhausted."
    }
    [void][IO.Directory]::CreateDirectory($OUTPUT_FOLDER)
    $result = if (-not $Fresh) { Get-OfflineAssessmentResult } else { $null }
    $reused = $null -ne $result
    if ($reused -and $PerformanceDataPath) {
        $result = Invoke-SqlRecommendations -Suitability $result
    } else {
        $result = Invoke-OfflineTargetAssessment -ExistingResult $result
    }
    [PSCustomObject]@{
        InstanceName = $result.Report.Servers[0].Properties.ServerName
        OutputDirectory = $OUTPUT_FOLDER
        JsonPath = $result.JsonPath
        HtmlPath = $result.HtmlPath
        GeneratedAtUtc = $result.GeneratedAtUtc
        Reused = $reused
        SizingCriterion = $result.SizingCriterion
        SkuJsonPaths = $result.SkuJsonPaths
        SuitabilityHtmlPath = $result.SuitabilityHtmlPath
    } | ConvertTo-Json -Depth 5 -Compress
} catch {
    [Console]::Error.WriteLine((Get-SafeSqlDiagnostic $_))
    exit 1
}
