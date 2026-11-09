function Invoke-SqlTool {
    param(
        [string]$Executable,
        [string[]]$Arguments,
        [string]$Stage,
        [int]$TimeoutMilliseconds = 30000
    )

    $start = [Diagnostics.ProcessStartInfo]::new()
    if ($Executable -match '\.(cmd|bat)$') {
        if (@(@($Executable) + $Arguments | Where-Object { $_ -match '["%\r\n]' }).Count) {
            throw "[evaluate-offline-sql-migration-readiness] INVALID_PARAMETERS: Stage ${Stage}: command paths or selectors contain unsupported shell characters."
        }
        $quoted = @(@($Executable) + $Arguments | ForEach-Object { '"' + $_ + '"' })
        $start.FileName = $env:ComSpec
        $start.Arguments = '/d /v:off /s /c "' + ($quoted -join ' ') + '"'
    } else {
        $start.FileName = $Executable
        foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    }
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $started = $false
    try {
        [void]$process.Start()
        $started = $true
        $process.StandardInput.Close()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "[evaluate-offline-sql-migration-readiness] EXECUTION_TIMEOUT: Stage ${Stage}: command exceeded its execution budget; no completed result was confirmed."
        }
        return [PSCustomObject]@{
            ExitCode = $process.ExitCode
            Output = $stdout.GetAwaiter().GetResult()
            Error = $stderr.GetAwaiter().GetResult()
        }
    } catch {
        if ($_.Exception.Message.StartsWith('[evaluate-offline-sql-migration-readiness] ')) { throw }
        throw "[evaluate-offline-sql-migration-readiness] ASSESSMENT_FAILED: Stage ${Stage}: process could not start or complete; no exit code was confirmed. Verify local executable and file access."
    } finally {
        if ($started -and -not $process.HasExited) { $process.Kill($true) }
        $process.Dispose()
    }
}

function Get-SqlFailureCategory([string]$Text) {
    if ($Text -match '(?i)login failed|LoginFailure|AADSTS\d+|authentication failed|credential.*unavailable') {
        return @{ Code = 'AUTHENTICATION_REQUIRED'; Reason = 'Existing noninteractive SQL authentication failed. Prepare access outside assessment; no alternate authentication was attempted.' }
    }
    if ($Text -match '(?i)permission.*denied|access.*denied|PermissionDenied|AccessDenied') {
        return @{ Code = 'ACCESS_DENIED'; Reason = 'SQL or file permissions are insufficient. Prepare permissions outside assessment.' }
    }
    if ($Text -match '(?i)certificate|SSL Provider|TLS handshake') {
        return @{ Code = 'ASSESSMENT_FAILED'; Reason = 'A certificate or TLS failure was reported. Verify the configured trust and encryption outside assessment; encryption was not disabled.' }
    }
    if ($Text -match '(?i)network-related|server was not found|connection.*(?:timed out|refused)|TCP Provider|No such host|error:\s*(?:26|40|53)\b') {
        return @{ Code = 'ASSESSMENT_FAILED'; Reason = 'A SQL connectivity failure was reported. Verify the endpoint, listener and network access outside assessment.' }
    }
    return $null
}
