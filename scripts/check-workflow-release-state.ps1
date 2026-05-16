$ErrorActionPreference = "Stop"

$ciPath = ".github/workflows/ci.yml"
$releasePath = ".github/workflows/release-preview.yml"

if (!(Test-Path -LiteralPath $ciPath)) {
    throw "Missing CI workflow: $ciPath"
}

if (!(Test-Path -LiteralPath $releasePath)) {
    throw "Missing release preview workflow: $releasePath"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
$release = Get-Content -LiteralPath $releasePath -Raw

$forbiddenCi = @(
    "PriorityGear-v0.1.0-win-x64-framework-dependent",
    "PriorityGear-v0.1.0-win-x64-self-contained",
    "Publish framework-dependent",
    "Publish self-contained single-file",
    "Upload framework-dependent artifact",
    "Upload self-contained artifact"
)

foreach ($needle in $forbiddenCi) {
    if ($ci.Contains($needle)) {
        throw "CI workflow still contains stale release artifact logic: $needle"
    }
}

$requiredRelease = @(
    "v*-preview.*",
    "--verify-tag",
    "package-preview-release.ps1",
    "inspect-release-artifacts.ps1"
)

foreach ($needle in $requiredRelease) {
    if (!$release.Contains($needle)) {
        throw "Release Preview workflow is missing required text: $needle"
    }
}

if (!$release.Contains('"release", "create"') -or !$release.Contains("gh @releaseArgs")) {
    throw "Release Preview workflow must create releases through a gh argument array."
}

if ($release.Contains("v0.2.0-preview.1")) {
    throw "Release Preview workflow contains a stale fixed preview tag."
}

if ($release.Contains("--target")) {
    throw "Release Preview workflow should rely on the pushed tag and --verify-tag, not --target."
}

Write-Host "Workflow release state check passed."
