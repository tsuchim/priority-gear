param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDirectory,

    [Parameter(Mandatory = $true)]
    [string]$TagName
)

$ErrorActionPreference = "Stop"

if ($TagName -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+$' -or $TagName.Contains("..") -or $TagName.Contains("/") -or $TagName.Contains("\")) {
    throw "TagName must match v<major>.<minor>.<patch> and contain only safe filename characters."
}

$artifactPath = Resolve-Path -LiteralPath $ArtifactDirectory
$zipName = "PriorityGear-$TagName-win-x64-installer.zip"
$checksumName = "PriorityGear-$TagName-SHA256SUMS.txt"
$zipPath = Join-Path $artifactPath $zipName
$checksumPath = Join-Path $artifactPath $checksumName

if (!(Test-Path -LiteralPath $zipPath)) {
    throw "Missing zip artifact: $zipPath"
}

if (!(Test-Path -LiteralPath $checksumPath)) {
    throw "Missing checksum file: $checksumPath"
}

$expectedLine = Get-Content -LiteralPath $checksumPath | Select-Object -First 1
if ($expectedLine -notmatch "^([A-Fa-f0-9]{64})\s+$([Regex]::Escape($zipName))$") {
    throw "Checksum file is malformed or does not reference $zipName."
}

$expectedHash = $Matches[1].ToUpperInvariant()
$actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
if ($actualHash -ne $expectedHash) {
    throw "Checksum mismatch. Expected $expectedHash but got $actualHash."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName })
    $required = @(
        "PriorityGear.Setup.exe",
        "setup-version.txt",
        "winget-install.json",
        "payload/PriorityGear.Service.exe",
        "payload/PriorityGear.Cli.exe",
        "payload/PriorityGear.App.exe"
    )

    foreach ($entry in $required) {
        if ($entries -notcontains $entry) {
            throw "Zip is missing required entry: $entry"
        }
    }

    $versionEntry = $zip.GetEntry("setup-version.txt")
    if ($null -eq $versionEntry) {
        throw "Zip is missing setup-version.txt."
    }

    $reader = New-Object System.IO.StreamReader($versionEntry.Open())
    try {
        $setupVersion = $reader.ReadToEnd().Trim()
    }
    finally {
        $reader.Dispose()
    }

    if ($setupVersion -ne $TagName) {
        throw "setup-version.txt must match tag $TagName but was $setupVersion."
    }

    $wingetEntry = $zip.GetEntry("winget-install.json")
    if ($null -eq $wingetEntry) {
        throw "Zip is missing winget-install.json."
    }

    $reader = New-Object System.IO.StreamReader($wingetEntry.Open())
    try {
        $wingetMetadata = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    if ($wingetMetadata -notmatch '"nestedInstallerFile"\s*:\s*"PriorityGear\.Setup\.exe"' -or
        $wingetMetadata -notmatch '"silentInstall"\s*:\s*"--install --silent"' -or
        $wingetMetadata -notmatch '"silentUninstall"\s*:\s*"--uninstall --silent"') {
        throw "winget-install.json must declare PriorityGear.Setup.exe and silent install/uninstall switches."
    }

    $forbidden = $entries | Where-Object {
        $_ -like ".git/*" -or
        $_ -like "*/.git/*" -or
        $_ -like "src/*" -or
        $_ -like "tests/*" -or
        $_ -like "*.Tests.dll"
    }

    if ($forbidden) {
        $badList = ($forbidden | Select-Object -First 20) -join [Environment]::NewLine
        throw "Zip contains forbidden entries:$([Environment]::NewLine)$badList"
    }
}
finally {
    $zip.Dispose()
}

Write-Host "Release artifact inspection passed."
Write-Host "  Zip: $zipPath"
Write-Host "  SHA-256: $actualHash"
