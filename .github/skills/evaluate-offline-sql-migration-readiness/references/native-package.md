# Prepare the native tool

The skill owns tool selection, not the credential-bearing assessment helper.
An explicit `SqlAssessmentPath` pins that executable; reject simultaneous forced
refresh before doing any work. For a default tool, use the commands below in the
local PowerShell tool session, then call `Get-SqlAssessmentPackage` and retain its
non-secret returned paths and package evidence. Set `ASSESSMENT_DEADLINE_UTC` to
the shared seven-minute deadline first. Do not pipe this block to stdin, extract
Markdown at runtime, generate a workflow script, or use `Invoke-Expression`.

This preparation retains the existing cache checks: fresh remote strong identity,
local content hash, trusted download origin, safe archive extraction and isolated
staging. It is not a reason to package the skill's orchestration.
Use `-ForceRefresh` only for explicit refresh intent. After all targets and
publication, always call `Remove-SqlAssessmentPackage` for the returned staging
directory, including on failure. Never remove an explicitly supplied build.

<!-- native-package-commands -->
```powershell
function Invoke-SqlPackageRequest {
    param([string]$Method, [string]$Destination)

    $remaining = [int]($ASSESSMENT_DEADLINE_UTC - [DateTime]::UtcNow).TotalSeconds
    if ($remaining -le 0) {
        throw '[evaluate-offline-sql-migration-readiness] EXECUTION_TIMEOUT: Stage native-package: the shared execution budget is exhausted.'
    }
    $arguments = @{
        Uri = 'https://aka.ms/sqlassessmentpackage'
        Method = $Method
        Headers = @{ 'Cache-Control' = 'no-cache' }
        TimeoutSec = [Math]::Min(120, $remaining)
        ErrorAction = 'Stop'
    }
    if ($Destination) {
        $arguments.OutFile = $Destination
        $arguments.PassThru = $true
    }
    try {
        $response = Invoke-WebRequest @arguments
        $uri = $response.BaseResponse.RequestMessage.RequestUri
        if ($uri.Scheme -ne 'https' -or $uri.Host -ne 'download.microsoft.com') {
            throw 'Unexpected package origin.'
        }
        [PSCustomObject]@{ ETag = [string]($response.Headers.ETag -join '') }
    } catch {
        throw '[evaluate-offline-sql-migration-readiness] PACKAGE_DOWNLOAD_FAILED: Stage native-package: the Microsoft download-center freshness check or package download failed. Verify HTTPS access outside assessment; no stale executable was used.'
    }
}

function Remove-SqlAssessmentPackage {
    param([string]$StagingDirectory)

    $directory = [IO.Path]::GetFullPath($StagingDirectory)
    if ([IO.Path]::GetDirectoryName($directory) -ine [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetTempPath()) -or
        [IO.Path]::GetFileName($directory) -notmatch '^modernize-sql-package-[0-9a-f]{32}$') {
        throw '[evaluate-offline-sql-migration-readiness] CLEANUP_FAILED: Stage native-package: refusing to remove an unowned package directory.'
    }
    try {
        if ([IO.Directory]::Exists($directory)) { [IO.Directory]::Delete($directory, $true) }
    } catch {
        throw '[evaluate-offline-sql-migration-readiness] CLEANUP_FAILED: Stage native-package: invocation-local package files could not be removed. Inspect the current user temporary directory outside assessment.'
    }
}

function Get-SqlAssessmentPackage {
    param([switch]$ForceRefresh)

    $directory = Join-Path ([IO.Path]::GetTempPath()) ('modernize-sql-package-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($directory)
    $completed = $false
    $cacheTemporary = $null
    $metadataTemporary = $null
    try {
        $remote = Invoke-SqlPackageRequest 'Head'
        $cache = Join-Path $env:LOCALAPPDATA 'Microsoft\SqlAssessment\Modernize\PackageCache'
        $cachedArchive = Join-Path $cache 'SqlAssessment.zip'
        $metadataPath = Join-Path $cache 'package.json'
        $archivePath = Join-Path $directory 'SqlAssessment.zip'
        $reused = $false
        if (-not $ForceRefresh -and $remote.ETag -match '^"[^"]+"$' -and
            [IO.File]::Exists($metadataPath) -and [IO.File]::Exists($cachedArchive)) {
            try {
                $metadata = [IO.File]::ReadAllText($metadataPath) | ConvertFrom-Json -ErrorAction Stop
            } catch {
                throw '[evaluate-offline-sql-migration-readiness] PACKAGE_INVALID: Stage native-package: cached package metadata is unreadable. Force a package refresh; no unverifiable cached executable was used.'
            }
            if ($metadata.Source -eq 'https://aka.ms/sqlassessmentpackage' -and $metadata.ETag -ceq $remote.ETag) {
                [IO.File]::Copy($cachedArchive, $archivePath, $false)
                $reused = $metadata.Sha256 -ceq (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
            }
        }
        if (-not $reused) {
            $remote = Invoke-SqlPackageRequest 'Get' $archivePath
        }
        $hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
        $extracted = Join-Path $directory 'package'
        try {
            [IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $extracted)
            $executables = @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Filter 'SqlAssessment.exe')
            if ($executables.Count -ne 1 -or $executables[0].Length -eq 0) {
                throw 'Expected one native executable.'
            }
        } catch {
            throw '[evaluate-offline-sql-migration-readiness] PACKAGE_INVALID: Stage native-package: the published ZIP could not be safely extracted or did not contain exactly one nonempty SqlAssessment.exe. No cached executable was substituted.'
        }
        if (-not $reused) {
            [void][IO.Directory]::CreateDirectory($cache)
            $cacheTemporary = Join-Path $cache ([Guid]::NewGuid().ToString('N') + '.zip.tmp')
            $metadataTemporary = Join-Path $cache ([Guid]::NewGuid().ToString('N') + '.json.tmp')
            [IO.File]::Copy($archivePath, $cacheTemporary, $false)
            [IO.File]::WriteAllText($metadataTemporary, (@{
                Source = 'https://aka.ms/sqlassessmentpackage'
                ETag = $remote.ETag
                Sha256 = $hash
            } | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
            [IO.File]::Move($cacheTemporary, $cachedArchive, $true)
            [IO.File]::Move($metadataTemporary, $metadataPath, $true)
        }
        $completed = $true
        [PSCustomObject]@{
            ExecutablePath = $executables[0].FullName
            StagingDirectory = $directory
            Source = 'MicrosoftDownloadCenter'
            Sha256 = $hash
            ETag = $remote.ETag
            CacheReused = $reused
        }
    } finally {
        foreach ($path in @($cacheTemporary, $metadataTemporary)) {
            if ($path -and [IO.File]::Exists($path)) { [IO.File]::Delete($path) }
        }
        if (-not $completed) { Remove-SqlAssessmentPackage $directory }
    }
}
```
