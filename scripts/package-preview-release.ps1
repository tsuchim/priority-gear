param(
    [Parameter(Mandatory = $true)]
    [string]$TagName,

    [Parameter(Mandatory = $true)]
    [string]$SetupDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"

if ($TagName -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+$' -or $TagName.Contains("..") -or $TagName.Contains("/") -or $TagName.Contains("\")) {
    throw "TagName must match v<major>.<minor>.<patch> and contain only safe filename characters."
}

$setupPath = Resolve-Path -LiteralPath $SetupDirectory
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$outputPath = Resolve-Path -LiteralPath $OutputDirectory

$zipName = "PriorityGear-$TagName-system-mode-verification.zip"
$checksumName = "PriorityGear-$TagName-SHA256SUMS.txt"
$zipPath = Join-Path $outputPath $zipName
$checksumPath = Join-Path $outputPath $checksumName

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $checksumPath -Force -ErrorAction SilentlyContinue

$badEntries = Get-ChildItem -LiteralPath $setupPath -Recurse -Force | Where-Object {
    $_.FullName -like "*\.git\*" -or
    $_.FullName -like "*\src\*" -or
    $_.FullName -like "*\tests\*" -or
    $_.Name -like "*.Tests.dll"
}

if ($badEntries) {
    $badList = ($badEntries | Select-Object -First 20 -ExpandProperty FullName) -join [Environment]::NewLine
    throw "Setup directory contains files that must not be packaged:$([Environment]::NewLine)$badList"
}

Compress-Archive -Path (Join-Path $setupPath "*") -DestinationPath $zipPath -Force

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
Set-Content -LiteralPath $checksumPath -Value "$($hash.Hash)  $zipName" -Encoding ascii

Write-Host "Release artifacts created:"
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
