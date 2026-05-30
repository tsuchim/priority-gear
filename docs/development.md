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

## Validation Safety

- Never terminate or restart a process that the validator did not create specifically for the test.
- Controllable target process does not mean disposable test process.
- System infrastructure processes such as `vmmemWSL.exe`, Docker Desktop, database containers, and VS Code remote hosts must be treated as live user infrastructure.
- For destructive validation, create a dedicated test process or test service and operate only on that.
- Cleanup must prefer rule deletion or disablement and explicit state restoration, not process termination.
- Any operation that may stop workloads must be reported as a blocker and must not be executed by default.

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

The JSON output lists each physical core index, logical processor mask, raw Windows `EfficiencyClass`, processor group, whether PriorityGear sees heterogeneous efficiency data, and plans for `CoreReserve = 0`, `1`, `2`, and an invalid value. Each plan reports whether it succeeded, the allowed logical-processor mask, the reserved logical-processor mask, the reserved physical-core indexes, and a human-readable message.

`CoreReserve = 0` means no affinity change. Nonzero values exclude that many physical cores from the target process affinity; those cores remain available to Windows and other processes. On heterogeneous systems, higher raw `EfficiencyClass` cores are excluded first. Multiple processor groups are rejected because the current affinity path uses a single process affinity mask.

## Replacing a WSL / vmmem Core-Reserve Script

To reproduce a script that targets `vmmem*`, lowers priority, and leaves selected physical cores free:

1. Start WSL so the WSL VM process is present.
2. In PriorityGear, use the process-name filter to find the observed process name. On the validated Windows 11 machine, the WSL workload process was `vmmemWSL.exe`; `vmmemCmZygote` was also present but was not the preferred rule target.
3. Add a rule for that process.
4. Set Base priority to `BelowNormal`.
5. Leave Active priority as `Same as normal`.
6. Set `Core Reserve` to the number of physical cores to exclude from WSL, such as `1` or `2`.
7. Start monitoring.

If User Mode can mutate the process, the rule applies directly. On the validated machine, non-elevated PowerShell could observe `vmmemWSL` but could not read its priority or affinity, so System Mode is the expected path for this workload. If Windows denies priority or affinity mutation, install/use System Mode so the administrator-approved service can apply a matching machine rule. PriorityGear must show unsupported, denied, or failed states explicitly; it must not report success when topology or affinity application fails.

Verify the result with Task Manager, PowerShell process priority/affinity inspection, and `PriorityGear.Cli core-topology`. Compare masks with the old script by intent: PriorityGear uses Windows physical-core topology and reserves higher `EfficiencyClass` cores first, not WSL `lscpu` numbering.

For System Mode, run the CLI from an elevated administrator terminal because machine-rule mutations use the administrator-only pipe:

```powershell
& "C:\Program Files\PriorityGear\versions\<version>\PriorityGear.Cli.exe" machine-rules add `
  --name "WSL vmmem" `
  --exe vmmemWSL.exe `
  --priority BelowNormal `
  --core-reserve 2 `
  --approve
```

The installed CLI diagnostic on the validated Intel hybrid machine reported 20 physical cores, heterogeneous efficiency data, `EfficiencyClass = 1` for physical cores `0` through `7`, and `EfficiencyClass = 0` for physical cores `8` through `19`. The expected plans were:

- `CoreReserve = 1`: reserved mask `0x3`, allowed mask `0xFFFFFFC`, reserved core indexes `[0]`.
- `CoreReserve = 2`: reserved mask `0xF`, allowed mask `0xFFFFFF0`, reserved core indexes `[0, 1]`.

Installed-build System Mode validation used `C:\Program Files\PriorityGear\versions\v0.3.6` with service binary `C:\Program Files\PriorityGear\versions\v0.3.6\PriorityGear.Service.exe`. Rule `4bb5151a-a3c0-44fc-ac2a-0a40517d0214` targeted `vmmemWSL.exe` with Base priority `BelowNormal`; `vmmemCmZygote` was observed but was not targeted. `CoreReserve = 1` applied priority `BelowNormal` and affinity `0xFFFFFFC`. Updating only `CoreReserve` to `2` reapplied the installed System Mode rule and changed affinity to `0xFFFFFF0`.

Removing a rule does not necessarily restore priority or affinity on an already-running process. Normal validation cleanup is to delete or disable the PriorityGear test rule, then manually restore priority or affinity only when an explicit reset command is available and verified. If no verified reset is available, record that the already-running process may retain OS-level priority or affinity until the user naturally restarts that workload. Do not stop WSL, Docker, containers, databases, VS Code remote sessions, or other user workloads as routine cleanup. `wsl --shutdown` is a disruptive environment-wide manual reset that stops all WSL distributions and workloads under them; it must not be part of automated validation or routine cleanup instructions and requires explicit human approval outside the validation flow.

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
