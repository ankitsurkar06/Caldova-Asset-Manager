function Get-OfflineAssessmentResult {
  param(
    [datetime]$MinimumWriteTimeUtc = [datetime]::MinValue,
    [hashtable]$PreviousReportWriteTimes = $null
  )

  $freshRun = $MinimumWriteTimeUtc -ne [datetime]::MinValue
  if ($freshRun -and $null -eq $PreviousReportWriteTimes) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Fresh validation requires the pre-run report snapshot. No report was accepted."
  }
  if (-not (Test-Path -LiteralPath $OUTPUT_FOLDER)) { return $null }
  if (-not (Test-Path -LiteralPath $OUTPUT_FOLDER -PathType Container)) {
    throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: The supplied output destination must identify a directory."
  }
  try {
    $files = @(Get-ChildItem -LiteralPath $OUTPUT_FOLDER -File -ErrorAction Stop)
  } catch {
    throw "[evaluate-offline-sql-migration-readiness] ACCESS_DENIED: The supplied output folder cannot be read. Prepare file permissions outside this assessment."
  }
  $target = $INSTANCE -replace '\\', '_'
  $candidates = @(
    $files | Where-Object {
      $_.Extension -ieq ".json" -and $_.LastWriteTimeUtc -ge $MinimumWriteTimeUtc -and (
        ($_.BaseName -match '^SqlAssessment-(.+)-\d{8,}$' -and
          ($freshRun -or $Matches[1] -ieq $target)) -or
        $_.Name -like "SqlAssessmentReport-*.json"
      ) -and (
        -not $freshRun -or
        -not $PreviousReportWriteTimes.ContainsKey($_.FullName) -or
        $_.LastWriteTimeUtc.Ticks -gt $PreviousReportWriteTimes[$_.FullName]
      )
    } | Sort-Object LastWriteTimeUtc -Descending
  )
  if ($freshRun -and $candidates.Count -gt 1) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: More than one assessment JSON was written during this single-instance run. No unambiguous fresh result was accepted."
  }
  foreach ($candidate in $candidates) {
    try {
      $report = Get-Content -LiteralPath $candidate.FullName -Raw -ErrorAction Stop |
        ConvertFrom-Json -ErrorAction Stop
    } catch {
      throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: An assessment JSON candidate could not be read or parsed. Verify it outside assessment or explicitly request fresh results."
    }
    $servers = @($report.Servers)
    if ($servers.Count -ne 1 -or
        [string]::IsNullOrWhiteSpace([string]$servers[0].Properties.ServerName)) {
      $errorIds = @($report.Errors | ForEach-Object { [string]$_.ErrorID }) -join " "
      $failure = Get-SqlFailureCategory $errorIds
      if ($failure) { throw "[evaluate-offline-sql-migration-readiness] $($failure.Code): Stage artifact-validation: $($failure.Reason)" }
      throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Assessment JSON does not identify one SQL instance. No readiness conclusions were produced."
    }
    if (-not $freshRun -and -not [string]::Equals(
        [string]$servers[0].Properties.ServerName, $INSTANCE,
        [StringComparison]::OrdinalIgnoreCase)) {
      if ($candidate.Name -like "SqlAssessment-*") {
        throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: The instance-tagged report identifies a different SQL instance. Verify the output scope outside this assessment."
      }
      continue
    }
    if ($null -eq $servers[0].Databases) {
      throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Assessment JSON lacks database assessment data. No readiness conclusions were produced."
    }
    if (($report.Status -and $report.Status -ne 'Completed') -or
        ($servers[0].Status -and $servers[0].Status -ne 'Completed')) {
      throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage artifact-validation: assessment is incomplete; no readiness conclusions were produced."
    }
    $html = $files | Where-Object {
      $_.Extension -ieq ".html" -and $_.BaseName -ieq $candidate.BaseName
    } | Select-Object -First 1
    return [PSCustomObject]@{
      JsonPath = $candidate.FullName
      HtmlPath = if ($html) { $html.FullName } else { $null }
      GeneratedAtUtc = $candidate.LastWriteTimeUtc.ToString("o")
      Report = $report
    }
  }
  if ($MinimumWriteTimeUtc -eq [datetime]::MinValue -and
      ($files | Where-Object {
        $_.Extension -ieq ".html" -and (
          ($_.BaseName -match '^SqlAssessment-(.+)-\d{8,}$' -and $Matches[1] -ieq $target) -or
          $_.Name -like "SqlAssessmentReport-*.html"
        )
      })) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_DATA_UNAVAILABLE: Existing HTML has no matching usable assessment JSON. Verify the output or explicitly request a fresh assessment; no replacement was started."
  }
  return $null
}

function Invoke-OfflineAssessmentCommand {
  param(
    [Parameter(Mandatory)][string]$ConfigFilePath
  )

  $response = $null
  try {
    $waitMilliseconds = 420000
    if (Test-Path variable:ASSESSMENT_DEADLINE_UTC) {
      $waitMilliseconds = [Math]::Min($waitMilliseconds,
        [int]($ASSESSMENT_DEADLINE_UTC - [DateTime]::UtcNow).TotalMilliseconds)
    }
    if ($waitMilliseconds -le 0) {
      throw "[evaluate-offline-sql-migration-readiness] EXECUTION_TIMEOUT: Stage native-execution: the shared execution budget is exhausted."
    }
    $response = Invoke-SqlTool $SQL_ASSESSMENT_EXECUTABLE @('LoadFromJson', '--configFile', $ConfigFilePath) 'native-execution' $waitMilliseconds
    $failure = Get-SqlFailureCategory ($response.Output + "`n" + $response.Error)
    if ($failure) {
      throw "[evaluate-offline-sql-migration-readiness] $($failure.Code): Stage native-execution: native exit code $($response.ExitCode). $($failure.Reason)"
    }
    if ($response.ExitCode -ne 0) {
      throw "[evaluate-offline-sql-migration-readiness] ASSESSMENT_FAILED: Stage native-execution: native exit code $($response.ExitCode); unclassified command failure. Verify tool health outside assessment; raw output was withheld."
    }
    return $response.ExitCode
  } finally {
    $response = $null
  }
}

function Get-OfflineConnectionDataSource {
  param([Parameter(Mandatory)][string]$Value)

  $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
  try {
    $builder.psbase.ConnectionString = $Value
    $sources = @(
      "Data Source", "Server", "Address", "Addr", "Network Address" |
        Where-Object { $builder.ContainsKey($_) } |
        ForEach-Object { [string]$builder[$_] } |
        Select-Object -Unique
    )
    if ($sources.Count -ne 1 -or [string]::IsNullOrWhiteSpace($sources[0])) {
      throw "Invalid source."
    }
    return $sources[0]
  } catch {
    throw "[evaluate-offline-sql-migration-readiness] INVALID_PARAMETERS: A protected sqlConnectionStrings entry has invalid connection syntax or an empty or ambiguous Data Source / Server. Correct the entry only in the file selected by prompt configFilePath; use one nonempty endpoint matching prompt instanceName (host, named instance or host,port). Do not paste the entry into the prompt."
  } finally {
    $builder.Clear()
  }
}

function New-OfflineConnectionString {
  $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
  $password = $null
  $credential = $null
  $secretJson = $null
  try {
    $builder["Data Source"] = $INSTANCE
    $builder["Initial Catalog"] = "master"
    $builder["Encrypt"] = $true
    $builder["TrustServerCertificate"] = $TRUST_SERVER_CERTIFICATE
    $builder["Connect Timeout"] = 30
    switch ($AUTH_MODE) {
      "Windows" { $builder["Integrated Security"] = $true }
      "EntraDefault" {
        $builder["Authentication"] = "Active Directory Default"
      }
      "KeyVault" {
        $secretResponse = Invoke-SqlTool $AZ_EXECUTABLE @('keyvault', 'secret', 'show', '--vault-name', $KEY_VAULT_NAME, '--name', $SECRET_NAME, '--query', 'value', '-o', 'json', '--only-show-errors') 'credential-resolution'
        if ($secretResponse.ExitCode -ne 0 -or -not $secretResponse.Output) {
          throw "[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: Stage credential-resolution: Key Vault command exit code $($secretResponse.ExitCode). Existing authorized Azure access to the selected SQL-password secret is required."
        }
        $secretJson = $secretResponse.Output
        try {
          $password = ($secretJson -join [Environment]::NewLine) | ConvertFrom-Json -ErrorAction Stop
        } catch {
          throw "[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: The selected Key Vault secret could not supply a usable SQL password. Prepare it outside assessment."
        }
        if ($password -isnot [string] -or [string]::IsNullOrEmpty($password)) {
          throw "[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: The selected Key Vault secret must contain a nonempty SQL password. Prepare it outside assessment."
        }
        $builder["User ID"] = $SQL_USER_NAME
        $builder["Password"] = [string]$password
      }
      "CredentialManager" {
        if (-not (Get-Command Get-StoredCredential -ErrorAction SilentlyContinue)) {
          throw "[evaluate-offline-sql-migration-readiness] PREREQUISITE_REQUIRED: The CredentialManager module must already be available. Prepare it outside this assessment, then rerun."
        }
        try {
          $credential = Get-StoredCredential -Target $CREDENTIAL_TARGET -ErrorAction Stop
          if (-not $credential -or [string]::IsNullOrWhiteSpace($credential.UserName)) {
            throw "Missing credential."
          }
          $password = $credential.GetNetworkCredential().Password
          if ([string]::IsNullOrEmpty($password)) { throw "Missing password." }
        } catch {
          throw "[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: An existing accessible Credential Manager entry with SQL username and password is required. Prepare it outside this assessment, then rerun."
        }
        $builder["User ID"] = $credential.UserName
        $builder["Password"] = $password
      }
      default {
        throw "[evaluate-offline-sql-migration-readiness] INVALID_PARAMETERS: Prompt authMode must be Windows, EntraDefault, ConfigFile, KeyVault or CredentialManager. With prompt configFilePath, use ConfigFile or omit authMode; without either selector, Windows is the default."
      }
    }
    return $builder.psbase.ConnectionString
  } finally {
    $builder.Clear()
    $password = $null
    $credential = $null
    $secretJson = $null
    $secretResponse = $null
  }
}

function Assert-OfflineConfigAuthentication {
  param([Parameter(Mandatory)][string]$Value)

  $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
  try {
    $builder.psbase.ConnectionString = $Value
    $authentication = if ($builder.ContainsKey("Authentication")) {
      [string]$builder["Authentication"]
    } else { "" }
    if ($authentication -match "interactive|device\s*code") {
      throw "[evaluate-offline-sql-migration-readiness] INVALID_PARAMETERS: Authentication in the selected protected sqlConnectionStrings entry cannot use interactive or device-code sign-in. Correct Authentication inside the file selected by prompt configFilePath to use supported noninteractive credentials outside assessment; do not paste the entry into the prompt."
    }
    $integrated = @("Integrated Security", "Trusted_Connection") |
      Where-Object { $builder.ContainsKey($_) -and [string]$builder[$_] -match "^(true|sspi)$" }
    if ($integrated -or ($authentication -and $authentication -notmatch "password")) {
      return
    }
    $users = @("User ID", "UserId", "UID", "User") |
      Where-Object { $builder.ContainsKey($_) -and -not [string]::IsNullOrWhiteSpace([string]$builder[$_]) }
    $passwords = @("Password", "PWD") |
      Where-Object { $builder.ContainsKey($_) -and -not [string]::IsNullOrEmpty([string]$builder[$_]) }
    if (-not $users -or -not $passwords) {
      $missingFields = @(
        if (-not $users) { "User ID" }
        if (-not $passwords) { "Password" }
      ) -join ", "
      throw "[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: Selected protected sqlConnectionStrings entry is missing $missingFields. Supply the missing nonempty SQL-login fields only inside the file selected by prompt configFilePath, or configure supported noninteractive Windows or Entra authentication. Never supply credentials in the prompt."
    }
  } finally {
    $builder.Clear()
  }
}

function Invoke-OfflineTargetAssessment {
param($ExistingResult)
if ([string]::IsNullOrWhiteSpace([string]$AUTH_MODE) -and
    [string]::IsNullOrWhiteSpace([string]$USER_CONFIG_PATH)) {
  $AUTH_MODE = "Windows"
}
$temporaryConfigPath = Join-Path ([IO.Path]::GetTempPath()) (
  "sql-migration-assessment-" + [Guid]::NewGuid().ToString("N") + ".json")
$connectionString = $null
$userConfig = $null
$configuration = $null
$overwrite = "true"
try {
  if ($AUTH_MODE -eq "ConfigFile") {
    try {
      $userConfig = Get-Content -LiteralPath $USER_CONFIG_PATH -Raw -ErrorAction Stop |
        ConvertFrom-Json -ErrorAction Stop
    } catch {
      throw "[evaluate-offline-sql-migration-readiness] AUTHENTICATION_REQUIRED: Prompt configFilePath must be an absolute Windows path to an existing readable protected JSON assessment file. Correct the path, file permissions or JSON syntax outside assessment; do not paste its contents into the prompt."
    }
    if (-not $userConfig -or $userConfig.action -ine "Assess") {
      throw "[evaluate-offline-sql-migration-readiness] INVALID_PARAMETERS: Protected JSON file selected by prompt configFilePath must contain action as the JSON string 'Assess'. Correct action in that file outside assessment, not in the prompt."
    }
    if ($null -eq $userConfig.sqlConnectionStrings -or @($userConfig.sqlConnectionStrings).Count -eq 0) {
      throw "[evaluate-offline-sql-migration-readiness] MISSING_PARAMETERS: sqlConnectionStrings is required inside the protected JSON file selected by prompt configFilePath. Supply a nonempty connection collection with exactly one Data Source or Server matching prompt instanceName; never paste connection strings into the prompt."
    }
    $matches = @(
      foreach ($value in @($userConfig.sqlConnectionStrings)) {
        if ([string]::IsNullOrWhiteSpace([string]$value)) {
          throw "[evaluate-offline-sql-migration-readiness] INVALID_PARAMETERS: Protected sqlConnectionStrings contains an empty entry. Correct or remove that entry in the file selected by prompt configFilePath; keep exactly one connection whose Data Source or Server matches prompt instanceName. Never paste the entry into the prompt."
        }
        $source = Get-OfflineConnectionDataSource -Value ([string]$value)
        if ([string]::Equals($source, $INSTANCE, [StringComparison]::OrdinalIgnoreCase)) {
          [string]$value
        }
      }
    )
    if ($matches.Count -eq 0) {
      throw "[evaluate-offline-sql-migration-readiness] CONFLICTING_PARAMETERS: Prompt instanceName does not match Data Source or Server in any protected sqlConnectionStrings entry selected by configFilePath. Correct instanceName in the prompt or the intended connection endpoint in the protected file to the same SQL endpoint (host, named instance or host,port). Do not paste connection strings into the prompt. No SQL instance was assessed."
    }
    if ($matches.Count -gt 1) {
      throw "[evaluate-offline-sql-migration-readiness] CONFLICTING_PARAMETERS: More than one protected sqlConnectionStrings entry matches prompt instanceName. Remove duplicate matching entries in the file selected by configFilePath so exactly one Data Source or Server matches; do not paste entries into the prompt. No SQL instance was assessed."
    }
    $connectionString = $matches[0]
    Assert-OfflineConfigAuthentication -Value $connectionString
    if ($userConfig.PSObject.Properties.Name -contains "overwrite") {
      $overwrite = $userConfig.overwrite
    }
  } else {
    $connectionString = New-OfflineConnectionString
  }
  $configuration = @{
    action = "Assess"
    outputFolder = $OUTPUT_FOLDER
    overwrite = $overwrite
    sqlConnectionStrings = @($connectionString)
  }
  $stream = $null
  $writer = $null
  try {
    $security = [Security.AccessControl.FileSecurity]::new()
    $security.SetAccessRuleProtection($true, $false)
    $security.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
      [Security.Principal.WindowsIdentity]::GetCurrent().User,
      [Security.AccessControl.FileSystemRights]::FullControl,
      [Security.AccessControl.AccessControlType]::Allow))
    $stream = [IO.FileSystemAclExtensions]::Create(
      [IO.FileInfo]::new($temporaryConfigPath),
      [IO.FileMode]::CreateNew,
      [Security.AccessControl.FileSystemRights]::FullControl,
      [IO.FileShare]::None, 4096, [IO.FileOptions]::None, $security)
    $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
    $writer.Write(($configuration | ConvertTo-Json -Depth 4))
    $writer.Flush()
  } catch {
    throw "[evaluate-offline-sql-migration-readiness] ACCESS_DENIED: A private temporary credential config could not be created or written. Prepare file permissions and storage outside this assessment."
  } finally {
    if ($writer) { $writer.Dispose() }
    if ($stream) { $stream.Dispose() }
  }
  if ($ExistingResult) {
    $freshResult = $ExistingResult
    $assessmentExitCode = 0
  } else {
  try {
    $previousReportWriteTimes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $OUTPUT_FOLDER -File -Filter '*.json' -ErrorAction Stop) {
      $previousReportWriteTimes[$file.FullName] = $file.LastWriteTimeUtc.Ticks
    }
  } catch {
    throw "[evaluate-offline-sql-migration-readiness] ACCESS_DENIED: Existing report files could not be recorded before assessment. No fresh assessment was started."
  }
  $startedAtUtc = [datetime]::UtcNow
  $assessmentExitCode = Invoke-OfflineAssessmentCommand -ConfigFilePath $temporaryConfigPath
  try {
    $freshResult = Get-OfflineAssessmentResult -MinimumWriteTimeUtc $startedAtUtc -PreviousReportWriteTimes $previousReportWriteTimes
  } catch {
    if ($_.Exception.Message.StartsWith('[evaluate-offline-sql-migration-readiness] ')) {
      throw "$($_.Exception.Message) Native exit code $assessmentExitCode."
    }
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage artifact-validation: native exit code $assessmentExitCode; output could not be validated. Raw errors were withheld."
  }
  if (-not $freshResult) {
    throw "[evaluate-offline-sql-migration-readiness] ASSESSMENT_FAILED: Stage artifact-validation: native exit code $assessmentExitCode, but no fresh assessment JSON was produced. Exit zero is insufficient; no stale or partial report was returned."
  }
  if ($freshResult.HtmlPath -and (Get-Item -LiteralPath $freshResult.HtmlPath).LastWriteTimeUtc -lt $startedAtUtc) {
    throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage artifact-validation: native exit code $assessmentExitCode; matching HTML predates this assessment."
  }
  }
  $freshResult = Invoke-SqlRecommendations -Suitability $freshResult -PrivateConfigPath $temporaryConfigPath
  $secretBuilder = [Data.Common.DbConnectionStringBuilder]::new()
  try {
    $secretBuilder.psbase.ConnectionString = $connectionString
    foreach ($key in @('Password', 'PWD', 'Access Token', 'AccessToken')) {
      if ($secretBuilder.ContainsKey($key) -and -not [string]::IsNullOrEmpty([string]$secretBuilder[$key])) {
        foreach ($artifact in @(Get-ChildItem -LiteralPath $OUTPUT_FOLDER -File -Recurse | Where-Object { $_.Extension -in @('.json', '.html', '.md') })) {
          if ([IO.File]::ReadAllText($artifact.FullName).Contains([string]$secretBuilder[$key])) {
            throw "[evaluate-offline-sql-migration-readiness] REPORT_INVALID: Stage artifact-validation: native exit code $assessmentExitCode; sensitive connection material was found in native output; no report may be published."
          }
        }
      }
    }
  } finally { $secretBuilder.Clear() }
  $freshResult
} finally {
  $connectionString = $null
  $userConfig = $null
  $configuration = $null
  $matches = $null
  $value = $null
  if (Test-Path -LiteralPath $temporaryConfigPath) {
    try {
      Remove-Item -LiteralPath $temporaryConfigPath -Force -ErrorAction Stop
    } catch {
      throw "[evaluate-offline-sql-migration-readiness] CLEANUP_FAILED: The named temporary credential config could not be removed. Inspect the current user's temporary directory outside this assessment."
    }
  }
}

}
