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

The CI workflow is restore/build/test only. Preview release packaging is handled by the tag-driven `Release Preview` workflow and the scripts under `scripts/`.

## Branches

- `main`: stable.
- `devel`: active development.

No other permanent branches should be used.

## Policy

Keep User Mode and System Mode separated by milestone. Do not add service behavior to v0.1 code paths. Failures must be visible and structured.
