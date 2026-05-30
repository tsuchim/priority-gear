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
- `Core Reserve` defaults to `0`. Nonzero values require valid Windows physical-core topology and affinity application. Invalid reserve counts or unsupported topology must be reported as failures, not treated as success. Windows `PROCESSOR_RELATIONSHIP.EfficiencyClass` is kept as a raw numeric value: all-zero means homogeneous or unavailable distinction; on heterogeneous systems, higher values are treated as higher-performance cores and reserved first.

## Core Reserve Diagnostics

Use the CLI diagnostic to inspect the same Windows topology and planner used by the app and service:

```powershell
dotnet run --project src/PriorityGear.Cli/PriorityGear.Cli.csproj --configuration Release -- core-topology
```

The JSON output lists each physical core index, logical processor mask, raw Windows `EfficiencyClass`, processor group, whether PriorityGear sees heterogeneous efficiency data, and the allowed affinity masks for `CoreReserve = 0`, `1`, `2`, and an invalid value.

`CoreReserve = 0` means no affinity change. Nonzero values exclude that many physical cores from the target process affinity; those cores remain available to Windows and other processes. On heterogeneous systems, higher raw `EfficiencyClass` cores are excluded first. Multiple processor groups are rejected because the current affinity path uses a single process affinity mask.

## Replacing a WSL / vmmem Core-Reserve Script

To reproduce a script that targets `vmmem*`, lowers priority, and leaves selected physical cores free:

1. Start WSL so the WSL VM process is present.
2. In PriorityGear, use the process-name filter to find the observed process name, commonly `vmmemWSL.exe` or another `vmmem*` process.
3. Add a rule for that process.
4. Set Base priority to `BelowNormal`.
5. Leave Active priority as `Same as normal`.
6. Set `Core Reserve` to the number of physical cores to exclude from WSL, such as `1` or `2`.
7. Start monitoring.

If User Mode can mutate the process, the rule applies directly. If Windows denies priority or affinity mutation, install/use System Mode so the administrator-approved service can apply a matching machine rule. PriorityGear must show unsupported, denied, or failed states explicitly; it must not report success when topology or affinity application fails.

Verify the result with Task Manager, PowerShell process priority/affinity inspection, and `PriorityGear.Cli core-topology`. Compare masks with the old script by intent: PriorityGear uses Windows physical-core topology and reserves higher `EfficiencyClass` cores first, not WSL `lscpu` numbering.

Known limitations:

- Multiple processor groups are rejected.
- Protected or elevated processes may require System Mode.
- GPU attribution remains unsupported unless a verified PID-attributed source is added later.

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
