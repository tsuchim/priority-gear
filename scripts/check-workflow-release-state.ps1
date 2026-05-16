$ErrorActionPreference = "Stop"

$ciPath = ".github/workflows/ci.yml"
$releasePath = ".github/workflows/release-preview.yml"

if (!(Test-Path -LiteralPath $ciPath)) {
    throw "Missing CI workflow: $ciPath"
}

if (!(Test-Path -LiteralPath $releasePath)) {
    throw "Missing release workflow: $releasePath"
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
    "v*.*.*",
    "^v[0-9]+\.[0-9]+\.[0-9]+$",
    "--verify-tag",
    'docs/release-drafts/$tag.md',
    "package-preview-release.ps1",
    "inspect-release-artifacts.ps1"
)

foreach ($needle in $requiredRelease) {
    if (!$release.Contains($needle)) {
        throw "Release workflow is missing required text: $needle"
    }
}

if (!$release.Contains('"release", "create"') -or !$release.Contains("gh @releaseArgs")) {
    throw "Release workflow must create releases through a gh argument array."
}

$staleTags = @(
    "v0.2.0-preview.1",
    "v0.2.1-preview.1"
)

foreach ($tag in $staleTags) {
    if ($release.Contains($tag)) {
        throw "Release workflow contains a stale fixed tag: $tag"
    }
}

if ($release.Contains("v*-preview.*")) {
    throw "Release workflow still requires preview-suffixed tags."
}

if ($release.Contains("--prerelease") -or $release.Contains("--draft")) {
    throw "Release workflow should publish a normal public release for plain semver tags."
}

if (!$release.Contains("Tag does not match safe release pattern")) {
    throw "Release workflow must reject non-semver and preview-suffixed tags with the safe release regex."
}

if ($release.Contains("--target")) {
    throw "Release workflow should rely on the pushed tag and --verify-tag, not --target."
}

Write-Host "Workflow release state check passed."
