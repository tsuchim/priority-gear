# Development

## Requirements

- Windows 11.
- .NET 10 SDK.
- Git.

## Build

```powershell
dotnet restore PriorityGear.slnx
dotnet build PriorityGear.slnx --configuration Release --no-restore
```

## Test

```powershell
dotnet test PriorityGear.slnx --configuration Release --no-build
```

The primary target architecture is x64.

## Run From Source

```powershell
dotnet run --project src/PriorityGear.App/PriorityGear.App.csproj --configuration Release
```

## Current UI Behavior

- The process grid supports case-insensitive process-name filtering.
- The `3s snapshot` action performs a single manual resource sample. It is not a continuous background monitor.
- CPU snapshot values are estimated from process CPU-time delta across the sampling interval.
- Disk I/O snapshot values are estimated from process I/O counter byte deltas across the sampling interval.
- GPU snapshot values are displayed only when process-attributed GPU data is supported and verified. Unsupported or unavailable GPU attribution is shown explicitly and is not converted to zero.
- Metric filters narrow the process grid to rows with measured positive CPU, GPU, or Disk I/O usage in the latest snapshot.
- Priority selectors are ordered from highest supported priority to lowest.
- New rules default active priority to `Same as normal`; existing rules with explicit active overrides keep that behavior.
- `Core Reserve` defaults to `0`. Nonzero values require valid Windows physical-core topology and affinity application. Invalid reserve counts or unsupported topology must be reported as failures, not treated as success.

## Portable Publish

Framework-dependent:

```powershell
dotnet publish src/PriorityGear.App/PriorityGear.App.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output artifacts/publish/PriorityGear-v0.1-win-x64-framework-dependent
```

Self-contained single-file:

```powershell
dotnet publish src/PriorityGear.App/PriorityGear.App.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  --output artifacts/publish/PriorityGear-v0.1-win-x64-self-contained
```

Zip the publish directories for portable distribution. Do not commit generated binaries.

```powershell
Compress-Archive -Path artifacts/publish/PriorityGear-v0.1-win-x64-framework-dependent/* `
  -DestinationPath artifacts/PriorityGear-v0.1-win-x64-framework-dependent.zip `
  -Force

Compress-Archive -Path artifacts/publish/PriorityGear-v0.1-win-x64-self-contained/* `
  -DestinationPath artifacts/PriorityGear-v0.1-win-x64-self-contained.zip `
  -Force
```

The CI workflow is restore/build/test only. Release packaging is handled by the tag-driven `Release` workflow and the scripts under `scripts/`.

## Branches

- `main`: stable.
- `devel`: active development.

No other permanent branches should be used.

## Policy

Keep User Mode and System Mode separated by milestone. Do not add service behavior to v0.1 code paths. Failures must be visible and structured.
