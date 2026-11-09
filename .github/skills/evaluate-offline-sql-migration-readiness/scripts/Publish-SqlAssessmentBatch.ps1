[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string[]]$NativeHtmlPaths,
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
$temporary = $null
try {
    $root = [IO.Path]::GetFullPath($OutputDirectory)
    if (-not [IO.Path]::IsPathFullyQualified($OutputDirectory) -or -not [IO.Directory]::Exists($root) -or
        ([IO.File]::GetAttributes($root) -band [IO.FileAttributes]::ReparsePoint) -or $NativeHtmlPaths.Count -eq 0) {
        throw 'Invalid publication inputs.'
    }
    $documents = [Collections.Generic.List[string]]::new()
    foreach ($path in $NativeHtmlPaths) {
        $fullPath = [IO.Path]::GetFullPath($path)
        $relative = [IO.Path]::GetRelativePath($root, $fullPath)
        if ([IO.Path]::IsPathRooted($relative) -or $relative -eq '..' -or $relative.StartsWith('..' + [IO.Path]::DirectorySeparatorChar)) {
            throw 'Native report is outside the invocation.'
        }
        $part = $root
        foreach ($segment in $relative.Split([IO.Path]::DirectorySeparatorChar)) {
            $part = Join-Path $part $segment
            if ([IO.File]::GetAttributes($part) -band [IO.FileAttributes]::ReparsePoint) { throw 'Filesystem links are not allowed.' }
        }
        $document = [IO.File]::ReadAllText($fullPath)
        if ($document -notmatch '(?i)<html(?:\s|>)' -or $document -match '(?i)@import\b') { throw 'Native HTML is invalid or imports external styles.' }
        $patterns = @(
            '(?is)<(?:script|link|img|image|iframe|source|audio|video|embed|object|use)\b[^>]*\b(?:src|href|poster|data)\s*=\s*(?:"(?<asset>[^"]*)"|''(?<asset>[^'']*)''|(?<asset>[^\s>]+))',
            '(?is)url\s*\(\s*(?:"(?<asset>[^"]*)"|''(?<asset>[^'']*)''|(?<asset>[^\s)]+))\s*\)'
        )
        foreach ($pattern in $patterns) {
            foreach ($match in [regex]::Matches($document, $pattern)) {
                $asset = $match.Groups['asset'].Value.Trim()
                if ($asset -and -not $asset.StartsWith('data:', [StringComparison]::OrdinalIgnoreCase) -and -not $asset.StartsWith('#')) {
                    throw 'Native HTML depends on an external asset.'
                }
            }
        }
        $documents.Add($document)
    }
    if ($ValidateOnly) { return }
    $temporary = Join-Path $root ('.sql-batch-' + [Guid]::NewGuid().ToString('N') + '.tmp')
    if ($documents.Count -eq 1) {
        [IO.File]::Copy($NativeHtmlPaths[0], $temporary, $false)
    } else {
        $sections = for ($index = 0; $index -lt $documents.Count; $index++) {
            $encoded = [Net.WebUtility]::HtmlEncode($documents[$index])
            $number = $index + 1
            "<section><h2>SQL instance $number</h2><iframe title=`"Native SQL readiness report $number`" sandbox=`"allow-scripts`" srcdoc=`"$encoded`"></iframe></section>"
        }
        $html = '<!doctype html><html lang="en"><head><meta charset="utf-8"><title>SQL Server Migration Readiness</title><style>body{font-family:system-ui;margin:1.5rem}iframe{width:100%;height:90vh;border:1px solid #ccc}section{margin-bottom:2rem}</style></head><body><h1>SQL Server Migration Readiness</h1>' +
            ($sections -join "`n") + '</body></html>'
        [IO.File]::WriteAllText($temporary, $html, [Text.UTF8Encoding]::new($false))
    }
    [IO.File]::Move($temporary, (Join-Path $root 'index.html'), $true)
} catch {
    throw '[evaluate-offline-sql-migration-readiness] REPORT_PUBLICATION_FAILED: All verified native HTML reports must be regular self-contained files within the invocation. No complete batch report was published; raw report data and errors were withheld.'
} finally {
    if ($temporary -and [IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
}
