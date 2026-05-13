$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsRoot = Join-Path $RepoRoot "artifacts\setup-v0.2"
$PublishRoot = Join-Path $ArtifactsRoot "publish"
$DefaultSetupRoot = Join-Path $ArtifactsRoot "PriorityGear-v0.2-system-mode-verification"
$SetupRoot = $DefaultSetupRoot
$PayloadRoot = Join-Path $SetupRoot "payload"

Remove-Item -Recurse -Force $PublishRoot -ErrorAction SilentlyContinue
if (Test-Path $DefaultSetupRoot) {
    Remove-Item -Recurse -Force $DefaultSetupRoot -ErrorAction SilentlyContinue
    if (Test-Path $DefaultSetupRoot) {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $SetupRoot = Join-Path $ArtifactsRoot "PriorityGear-v0.2-system-mode-verification-$stamp"
        $PayloadRoot = Join-Path $SetupRoot "payload"
        Write-Warning "Default setup artifact directory is locked. Writing side-by-side artifact: $SetupRoot"
    }
}

New-Item -ItemType Directory -Path $PayloadRoot | Out-Null

function Publish-Project {
    param(
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][string]$Output
    )

    dotnet publish (Join-Path $RepoRoot $Project) `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $Output
}

Write-Host "Publishing verification setup..."
Publish-Project "src\PriorityGear.VerificationSetup\PriorityGear.VerificationSetup.csproj" (Join-Path $PublishRoot "setup")

Write-Host "Publishing service payload..."
Publish-Project "src\PriorityGear.Service\PriorityGear.Service.csproj" (Join-Path $PublishRoot "service")

Write-Host "Publishing CLI payload..."
Publish-Project "src\PriorityGear.Cli\PriorityGear.Cli.csproj" (Join-Path $PublishRoot "cli")

Write-Host "Publishing app payload..."
Publish-Project "src\PriorityGear.App\PriorityGear.App.csproj" (Join-Path $PublishRoot "app")

Write-Host "Publishing test target payload..."
Publish-Project "src\PriorityGear.TestTarget\PriorityGear.TestTarget.csproj" (Join-Path $PublishRoot "test-target")

Copy-Item -Path (Join-Path $PublishRoot "setup\*") -Destination $SetupRoot -Recurse -Force
Copy-Item -Path (Join-Path $PublishRoot "service\*") -Destination $PayloadRoot -Recurse -Force
Copy-Item -Path (Join-Path $PublishRoot "cli\*") -Destination $PayloadRoot -Recurse -Force
Copy-Item -Path (Join-Path $PublishRoot "app\*") -Destination $PayloadRoot -Recurse -Force
Copy-Item -Path (Join-Path $PublishRoot "test-target\*") -Destination $PayloadRoot -Recurse -Force

$required = @(
    (Join-Path $SetupRoot "PriorityGear.VerificationSetup.exe"),
    (Join-Path $PayloadRoot "PriorityGear.Service.exe"),
    (Join-Path $PayloadRoot "PriorityGear.Cli.exe"),
    (Join-Path $PayloadRoot "PriorityGear.App.exe"),
    (Join-Path $PayloadRoot "PriorityGear.TestTarget.exe")
)

foreach ($path in $required) {
    if (-not (Test-Path $path)) {
        throw "Required setup file is missing: $path"
    }
}

Write-Host ""
Write-Host "Verification setup artifact created:"
Write-Host "  $SetupRoot"
Write-Host ""
Write-Host "Double-click this file and approve UAC:"
Write-Host "  $(Join-Path $SetupRoot "PriorityGear.VerificationSetup.exe")"
