param(
    [Parameter(Mandatory = $true)]
    [string]$TagName,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"

if ($TagName -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+$' -or $TagName.Contains("..") -or $TagName.Contains("/") -or $TagName.Contains("\")) {
    throw "TagName must match v<major>.<minor>.<patch> and contain only safe filename characters."
}

$publishRoot = Join-Path "artifacts" "release-build"
$setupPublish = Join-Path $publishRoot "setup"
$payloadPublish = Join-Path $publishRoot "payload"
$stagingRoot = Join-Path $publishRoot "installer-staging"

Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $setupPublish -Force | Out-Null
New-Item -ItemType Directory -Path $payloadPublish -Force | Out-Null
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

Write-Host "Publishing installer..."
dotnet publish "src\PriorityGear.Setup\PriorityGear.Setup.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $setupPublish

Write-Host "Publishing service payload..."
dotnet publish "src\PriorityGear.Service\PriorityGear.Service.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $payloadPublish

Write-Host "Publishing CLI payload..."
dotnet publish "src\PriorityGear.Cli\PriorityGear.Cli.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $payloadPublish

Write-Host "Publishing app payload..."
dotnet publish "src\PriorityGear.App\PriorityGear.App.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $payloadPublish

Copy-Item -Path (Join-Path $setupPublish "*") -Destination $stagingRoot -Recurse -Force
Set-Content -LiteralPath (Join-Path $stagingRoot "setup-version.txt") -Value $TagName -Encoding ascii
Set-Content -LiteralPath (Join-Path $stagingRoot "winget-install.json") -Value (@"
{
  "installerType": "zip",
  "nestedInstallerType": "exe",
  "nestedInstallerFile": "PriorityGear.Setup.exe",
  "silentInstall": "--install --silent",
  "silentUninstall": "--uninstall --silent",
  "scope": "machine"
}
"@) -Encoding ascii
Copy-Item -LiteralPath $payloadPublish -Destination (Join-Path $stagingRoot "payload") -Recurse -Force

$required = @(
    "PriorityGear.Setup.exe",
    "setup-version.txt",
    "winget-install.json",
    "payload\PriorityGear.Service.exe",
    "payload\PriorityGear.Cli.exe",
    "payload\PriorityGear.App.exe"
)

foreach ($file in $required) {
    $path = Join-Path $stagingRoot $file
    if (!(Test-Path -LiteralPath $path)) {
        throw "Installer staging is missing required file: $file"
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$outputPath = Resolve-Path -LiteralPath $OutputDirectory
$zipName = "PriorityGear-$TagName-win-x64-installer.zip"
$checksumName = "PriorityGear-$TagName-SHA256SUMS.txt"
$zipPath = Join-Path $outputPath $zipName
$checksumPath = Join-Path $outputPath $checksumName

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $checksumPath -Force -ErrorAction SilentlyContinue

$badEntries = Get-ChildItem -LiteralPath $stagingRoot -Recurse -Force | Where-Object {
    $_.FullName -like "*\.git\*" -or
    $_.FullName -like "*\src\*" -or
    $_.FullName -like "*\tests\*" -or
    $_.Name -like "*.Tests.dll"
}

if ($badEntries) {
    $badList = ($badEntries | Select-Object -First 20 -ExpandProperty FullName) -join [Environment]::NewLine
    throw "Installer staging contains files that must not be packaged:$([Environment]::NewLine)$badList"
}

Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -Force

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
Set-Content -LiteralPath $checksumPath -Value "$($hash.Hash)  $zipName" -Encoding ascii

Write-Host "Release installer artifacts created:"
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
