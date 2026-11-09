[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkspacePath,
    [string]$ConfigurationFile,
    [switch]$EndpointOnly
)

$ErrorActionPreference = 'Stop'
$targets = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
$createdFiles = [Collections.Generic.List[string]]::new()
$privateDirectory = $null
$completed = $false

function Stop-Discovery([string]$Code, [string]$Reason) {
    throw "[evaluate-offline-sql-migration-readiness] ${Code}: $Reason"
}

function Add-SqlTarget([string]$Value, [bool]$EndpointSetting = $false) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return }
    $builder = [Data.Common.DbConnectionStringBuilder]::new()
    try {
        if ($Value -match '(?i)(?:^|;)\s*(?:Data Source|Server|Address|Addr|Network Address)\s*=') {
            try { $builder.psbase.ConnectionString = $Value } catch {
                Stop-Discovery 'INVALID_PARAMETERS' 'An application SQL connection string is malformed. Correct its source configuration without pasting it into the prompt.'
            }
            $sources = @(
                'Data Source', 'Server', 'Address', 'Addr', 'Network Address' |
                    Where-Object { $builder.ContainsKey($_) } |
                    ForEach-Object { ([string]$builder[$_]).Trim() } |
                    Sort-Object -Unique
            )
            if ($sources.Count -ne 1) {
                Stop-Discovery 'CONFLICTING_PARAMETERS' 'An application connection has conflicting SQL endpoint aliases. Correct its configuration.'
            }
            [string]$instance = $sources[0]
        } elseif ($EndpointSetting) {
            $instance = $Value.Trim()
        } else { return }

        if ($instance -match '(?i)\.(?:database\.windows\.net|database\.usgovcloudapi\.net|database\.chinacloudapi\.cn)(?:,|$)') {
            Stop-Discovery 'UNSUPPORTED_SCOPE' 'Discovered Azure SQL endpoints are outside this offline skill. Use the Azure assessment workflow or explicitly select a local instance.'
        }
        if ($instance -notmatch '^(?:tcp:)?(?:[a-zA-Z0-9_.-]+|\[[0-9a-fA-F:]+\]|\(local(?:db)?\))(?:\\[a-zA-Z0-9_$.-]+)?(?:,\d{1,5})?$' -or
            $instance -match '(?i)(?:\.invalid(?:,|$)|[<>{}%]|your[_-]|placeholder|changeme)') { return }

        $authentication = if ($builder.ContainsKey('Authentication')) { [string]$builder['Authentication'] } else { '' }
        if (-not $EndpointOnly -and $authentication -match '(?i)interactive|device\s*code') {
            Stop-Discovery 'AUTHENTICATION_REQUIRED' 'Application SQL authentication requires interaction. Configure noninteractive access or explicitly select Windows authentication.'
        }
        $users = @('User ID', 'UserId', 'UID', 'User' | Where-Object { $builder.ContainsKey($_) -and -not [string]::IsNullOrWhiteSpace([string]$builder[$_]) })
        $passwords = @('Password', 'PWD' | Where-Object { $builder.ContainsKey($_) -and -not [string]::IsNullOrEmpty([string]$builder[$_]) })
        $integrated = @('Integrated Security', 'Trusted_Connection' | Where-Object { $builder.ContainsKey($_) -and [string]$builder[$_] -match '^(?i:true|sspi|yes)$' })
        $hasCredentials = -not $EndpointOnly -and -not $integrated -and (
            ($users.Count -gt 0 -and $passwords.Count -gt 0) -or
            ($authentication -and $authentication -notmatch '(?i)password')
        )
        if (-not $hasCredentials) {
            foreach ($key in @('User ID', 'UserId', 'UID', 'User', 'Password', 'PWD', 'Authentication', 'Trusted_Connection')) {
                [void]$builder.Remove($key)
            }
            $builder['Integrated Security'] = $true
        }
        foreach ($key in @('Server', 'Address', 'Addr', 'Network Address', 'Database')) { [void]$builder.Remove($key) }
        $builder['Data Source'] = $instance
        $builder['Initial Catalog'] = 'master'
        $builder['Encrypt'] = $true
        if (-not $builder.ContainsKey('TrustServerCertificate')) { $builder['TrustServerCertificate'] = $true }
        $builder['Connect Timeout'] = 30
        $canonical = [Data.Common.DbConnectionStringBuilder]::new()
        foreach ($key in ($builder.Keys | Sort-Object)) { $canonical[[string]$key] = [string]$builder[$key] }
        $connection = $canonical.psbase.ConnectionString
        $canonical.Clear()
        if ($targets.ContainsKey($instance)) {
            $previous = $targets[$instance]
            if ($previous.HasCredentials -and $hasCredentials -and $previous.Connection -cne $connection) {
                Stop-Discovery 'CONFLICTING_PARAMETERS' 'Multiple application credential configurations target the same SQL endpoint. Select the intended configuration explicitly.'
            }
            if ($previous.HasCredentials -or -not $hasCredentials) { return }
        }
        $targets[$instance] = @{ Connection = $connection; HasCredentials = $hasCredentials }
    } finally { $builder.Clear() }
}

function Test-EndpointKey([string]$Name) {
    return ($Name -replace '[_:.-]', '') -match '^(?i:server|servername|sqlserver|sqlservername|sqlinstance|sqlinstancename|sqlserverinstance|sqlhost|sqlhostname|instancename|datasource)$'
}

function Test-SecretKey([string]$Name) {
    return $Name -notmatch '(?i)connection.?string' -and $Name -match '(?i)(?:password|pwd|token|secret|user.?name|user.?id|uid)$'
}

function Read-SettingsValue($Value, [string]$Name = '') {
    if ($null -eq $Value) { return }
    if ($Value -is [string]) {
        if (Test-SecretKey $Name) { return }
        Add-SqlTarget $Value (Test-EndpointKey $Name)
    } elseif ($Value -is [Collections.IEnumerable] -and $Value -isnot [Collections.IDictionary]) {
        foreach ($item in $Value) { Read-SettingsValue $item $Name }
    } elseif ($Value -is [PSCustomObject]) {
        $endpointProperties = @($Value.PSObject.Properties | Where-Object { (Test-EndpointKey $_.Name) -and $_.Value -is [string] })
        if ($endpointProperties.Count -gt 0) {
            $connection = [Data.Common.DbConnectionStringBuilder]::new()
            try {
                foreach ($property in $Value.PSObject.Properties) {
                    $key = $property.Name -replace '[_ .-]', ''
                    $mapped = switch -Regex ($key) {
                        '^(?i:server|servername|sqlserver|sqlservername|sqlinstance|sqlinstancename|sqlserverinstance|sqlhost|sqlhostname|instancename|datasource)$' { 'Data Source'; break }
                        '^(?i:user|username|userid|uid)$' { 'User ID'; break }
                        '^(?i:password|pwd)$' { 'Password'; break }
                        '^(?i:integratedsecurity|trustedconnection)$' { 'Integrated Security'; break }
                        '^(?i:authentication)$' { 'Authentication'; break }
                        '^(?i:trustservercertificate)$' { 'TrustServerCertificate'; break }
                    }
                    if ($mapped -and ($property.Value -is [ValueType] -or $property.Value -is [string])) {
                        if ($connection.ContainsKey($mapped) -and [string]$connection[$mapped] -ine [string]$property.Value) {
                            Stop-Discovery 'CONFLICTING_PARAMETERS' 'Application SQL settings contain conflicting aliases. Correct their source configuration.'
                        }
                        $connection[$mapped] = [string]$property.Value
                    }
                }
                Add-SqlTarget $connection.psbase.ConnectionString
            } finally { $connection.Clear() }
        }
        foreach ($property in $Value.PSObject.Properties) { Read-SettingsValue $property.Value $property.Name }
    }
}

function Convert-SourceLiteral([string]$Literal) {
    if ($Literal.StartsWith('@"')) { return $Literal.Substring(2, $Literal.Length - 3).Replace('""', '"') }
    if ($Literal.StartsWith("'")) { return $Literal.Substring(1, $Literal.Length - 2).Replace("''", "'") }
    try { return ConvertFrom-Json -InputObject $Literal -ErrorAction Stop } catch {
        Stop-Discovery 'INVALID_PARAMETERS' 'A SQL source setting uses unsupported string escaping. Supply a concrete instance or supported application configuration.'
    }
}

function Read-ApplicationFile([IO.FileInfo]$File) {
    $text = [IO.File]::ReadAllText($File.FullName)
    switch -Regex ($File.Extension) {
        '^(?i:\.json)$' {
            try { $settings = ConvertFrom-Json -InputObject $text -ErrorAction Stop } catch {
                Stop-Discovery 'INVALID_PARAMETERS' 'An application JSON configuration could not be parsed. Correct the file before automatic SQL discovery.'
            }
            Read-SettingsValue $settings
            return
        }
        '^(?i:\.config|\.xml)$' {
            $settings = [Xml.XmlReaderSettings]::new()
            $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
            $settings.XmlResolver = $null
            $reader = $null
            try {
                $reader = [Xml.XmlReader]::Create($File.FullName, $settings)
                $document = [Xml.XmlDocument]::new()
                $document.XmlResolver = $null
                $document.Load($reader)
                foreach ($node in $document.SelectNodes('//@*|//text()')) {
                    $element = if ($node -is [Xml.XmlAttribute]) { $node.OwnerElement } else { $node.ParentNode }
                    if ($element.Attributes['providerName'] -and $element.Attributes['providerName'].Value -notmatch '(?i)SqlClient') { continue }
                    $name = $node.LocalName
                    if ($name -eq 'value' -and $element.Attributes['key']) { $name = $element.Attributes['key'].Value }
                    if (Test-SecretKey $name) { continue }
                    Add-SqlTarget $node.Value (Test-EndpointKey $name)
                }
            } catch {
                if ($_.Exception.Message.StartsWith('[evaluate-offline-sql-migration-readiness] ')) { throw }
                Stop-Discovery 'INVALID_PARAMETERS' 'An application XML configuration could not be parsed safely.'
            } finally { if ($reader) { $reader.Dispose() } }
            return
        }
        '^(?i:\.cs|\.vb|\.java|\.js|\.ts)$' {
            $literal = '@?"(?:\\.|""|[^"\\])*"|''(?:\\.|[^''\\])*'''
            $text = [regex]::Replace($text, "$literal|//[^\r\n]*|/\*[\s\S]*?\*/", {
                param($match)
                if ($match.Value.StartsWith('//') -or $match.Value.StartsWith('/*')) { return '' }
                return $match.Value
            })
            $environment = '(?:Environment\.GetEnvironmentVariable|System\.getenv)\(\s*"(?<key>[^"]+)"\s*\)(?:\s*\?\?\s*(?<fallback>' + $literal + '))?'
            foreach ($match in [regex]::Matches($text, $environment)) {
                $key = $match.Groups['key'].Value
                if (Test-SecretKey $key) { continue }
                $value = [Environment]::GetEnvironmentVariable($key)
                if ($null -eq $value -and $match.Groups['fallback'].Success) { $value = Convert-SourceLiteral $match.Groups['fallback'].Value }
                Add-SqlTarget $value (Test-EndpointKey $key)
            }
            $text = [regex]::Replace($text, $environment, '')
            foreach ($match in [regex]::Matches($text, $literal)) {
                $raw = $match.Value
                if ($raw -notmatch '(?i)(?:Data Source|Server|Address|Addr|Network Address)\s*=') { continue }
                $prefix = $text.Substring(0, $match.Index)
                if ($prefix -match '(?i)\b(?<key>\w+)\s*[:=]\s*$' -and (Test-SecretKey $Matches['key'])) { continue }
                Add-SqlTarget (Convert-SourceLiteral $raw)
            }
            $assignment = '(?im)\b(?<key>server|serverName|sqlServer|sqlServerName|sqlInstance|sqlServerInstance|instanceName|dataSource)\s*[:=]\s*(?<value>' + $literal + ')'
            foreach ($match in [regex]::Matches($text, $assignment)) {
                Add-SqlTarget (Convert-SourceLiteral $match.Groups['value'].Value) $true
            }
            return
        }
        default {
            foreach ($line in $text -split '\r?\n') {
                if ($line -match '^\s*(?<key>[\w:.-]+)\s*[:=]\s*(?<value>.+?)\s*$') {
                    $key = $Matches['key']
                    $value = $Matches['value']
                    if (Test-SecretKey $key) { continue }
                    if ($value -match '^([''"]).*\1$') { $value = $value.Substring(1, $value.Length - 2) }
                    Add-SqlTarget $value (Test-EndpointKey $key)
                }
            }
        }
    }
}

try {
    if (-not $IsWindows) { Stop-Discovery 'UNSUPPORTED_HOST' 'Automatic offline SQL discovery requires Windows PowerShell 7.' }
    if (-not [IO.Path]::IsPathFullyQualified($WorkspacePath) -or -not [IO.Directory]::Exists($WorkspacePath) -or
        ([IO.File]::GetAttributes($WorkspacePath) -band [IO.FileAttributes]::ReparsePoint)) {
        Stop-Discovery 'INVALID_PARAMETERS' 'A regular absolute workspace directory is required.'
    }
    if ($ConfigurationFile) {
        if (-not [IO.Path]::IsPathFullyQualified($ConfigurationFile) -or -not [IO.File]::Exists($ConfigurationFile) -or
            ([IO.File]::GetAttributes($ConfigurationFile) -band [IO.FileAttributes]::ReparsePoint)) {
            Stop-Discovery 'INVALID_PARAMETERS' 'The explicit protected configuration must be a regular absolute file.'
        }
        try { $configuration = [IO.File]::ReadAllText($ConfigurationFile) | ConvertFrom-Json -ErrorAction Stop } catch {
            Stop-Discovery 'INVALID_PARAMETERS' 'The protected assessment configuration could not be read or parsed.'
        }
        if ($configuration.action -ine 'Assess' -or -not $configuration.sqlConnectionStrings) {
            Stop-Discovery 'INVALID_PARAMETERS' 'The protected assessment configuration requires action Assess and a nonempty sqlConnectionStrings collection.'
        }
        foreach ($value in @($configuration.sqlConnectionStrings)) { Add-SqlTarget $value }
    } else {
        $directories = [Collections.Generic.Stack[string]]::new()
        $directories.Push([IO.Path]::GetFullPath($WorkspacePath))
        $excluded = @('.git', '.github', '.agent-work', '.vs', 'bin', 'obj', 'node_modules', 'packages', 'vendor', 'dist', 'build', 'target', 'tests', 'test', '__tests__', 'fixtures', 'docs', 'examples')
        while ($directories.Count -gt 0) {
            foreach ($entry in Get-ChildItem -LiteralPath $directories.Pop() -Force -ErrorAction Stop) {
                if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
                if ($entry.PSIsContainer) {
                    if ($entry.Name -notin $excluded) { $directories.Push($entry.FullName) }
                } elseif (($entry.Extension -in @('.json', '.config', '.xml', '.yaml', '.yml', '.properties', '.ini', '.env', '.cs', '.vb', '.java', '.js', '.ts') -or $entry.Name -eq '.env') -and
                    $entry.Name -notmatch '(?i)(?:lock\.json$|\.deps\.json$|\.runtimeconfig\.json$|\.Test[s]?\.)') {
                    Read-ApplicationFile $entry
                }
            }
        }
    }
    if ($targets.Count -eq 0) {
        Stop-Discovery 'MISSING_PARAMETERS' 'No concrete SQL instance could be discovered. Supply instanceName in assessment context or configure an application SQL endpoint; unresolved placeholders are not targets.'
    }
    $privateDirectory = Join-Path ([IO.Path]::GetTempPath()) ('modernize-sql-discovery-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($privateDirectory)
    $security = [Security.AccessControl.DirectorySecurity]::new()
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $security.SetOwner($identity)
    $security.SetAccessRuleProtection($true, $false)
    $security.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
    Set-Acl -LiteralPath $privateDirectory -AclObject $security
    $resolved = @(
        $index = 0
        foreach ($instance in ($targets.Keys | Sort-Object)) {
            $index++
            $path = Join-Path $privateDirectory ('target-{0:d4}.json' -f $index)
            $createdFiles.Add($path)
            @{ action = 'Assess'; sqlConnectionStrings = @($targets[$instance].Connection) } |
                ConvertTo-Json | Set-Content -LiteralPath $path -Encoding utf8NoBOM
            @{ instanceName = $instance; configFilePath = $path; authentication = if ($targets[$instance].HasCredentials) { 'Application' } else { 'Windows' } }
        }
    )
    @{ targets = $resolved; privateDirectory = $privateDirectory } | ConvertTo-Json -Depth 4 -Compress
    $completed = $true
} catch {
    if ($_.Exception.Message.StartsWith('[evaluate-offline-sql-migration-readiness] ')) { throw $_.Exception.Message }
    Stop-Discovery 'DISCOVERY_FAILED' 'Application settings could not be read safely. Check local file access and supported configuration formats; raw configuration and errors were withheld.'
} finally {
    if (-not $completed -and $privateDirectory) {
        foreach ($path in $createdFiles) { if ([IO.File]::Exists($path)) { [IO.File]::Delete($path) } }
        [IO.Directory]::Delete($privateDirectory)
    }
    $targets.Clear()
    $configuration = $null
}
