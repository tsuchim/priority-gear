# PriorityGear

PriorityGear is a Windows 11 process-priority manager with foreground-aware rules.
It is inspired by the behavior category of older priority tools, including the Japanese freeware AutoGear, but it is a clean-room implementation.

## v0.1 User Mode

PriorityGear v0.1 is a usable User Mode application for normal Windows users. It does not require administrator rights.

- WPF GUI and tray presence.
- Per-user rules stored as plain JSON.
- Matching by executable name, full path, and optional path suffix.
- Base priority for inactive processes.
- Active priority when a process owns the foreground window.
- Periodic process scanning and foreground polling.
- Process grid with matched rule, desired priority, active state, last apply result, and status.
- Rule editing, deletion, enable/disable, and persistence.
- Visible logs with repeated failure throttling.
- Explicit failures when priority cannot be applied.

Rules are evaluated in creation order. The first matching enabled `CurrentUser` rule wins.

Closing the main window exits the app. Use the tray menu while the app is running for Open, Start monitoring, Stop monitoring, and Exit.

Rules are stored at `%LocalAppData%\PriorityGear\rules.json`.

## Build and Run

```powershell
dotnet restore PriorityGear.slnx
dotnet build PriorityGear.slnx --configuration Release --no-restore
dotnet test PriorityGear.slnx --configuration Release --no-build
dotnet run --project src/PriorityGear.App/PriorityGear.App.csproj --configuration Release
```

## Usability and Monitoring

The development branch after `v0.3.5` includes usability and monitoring updates:

- Process list filtering by partial process name, case-insensitive.
- A manual one-shot resource snapshot action. The snapshot samples for about 3 seconds and displays estimated per-process CPU, GPU, and Disk I/O usage where supported.
- CPU is calculated from process CPU-time delta over the sampling interval.
- Disk I/O is calculated from process I/O counter deltas over the sampling interval.
- GPU attribution is shown as unsupported unless PriorityGear can verify a Windows source that attributes GPU usage to process IDs. Unknown GPU data is not reported as zero.
- The process list can sort by process name, CPU snapshot, GPU snapshot, and Disk I/O snapshot, and can filter to processes with measured CPU, GPU, or Disk I/O usage.
- Priority selectors are ordered from highest to lowest supported priority.
- Active priority defaults to `Same as normal`, meaning foreground-active processes use the normal/base priority unless the rule explicitly chooses a separate active override.
- `Core Reserve` defaults to `0`. A finite nonzero value applies an affinity policy that leaves that many physical cores unused for processes managed by the rule. PriorityGear rejects invalid reserve counts and reports unsupported topology or affinity failures explicitly.

Core Reserve requires Windows physical-core topology and process affinity support. When P-core/E-core distinction is exposed, PriorityGear reserves P-cores first. If the distinction is unavailable, the result is reported as generic physical-core reservation rather than claimed as P-core-aware. Applying affinity to protected or elevated processes follows the same User Mode/System Mode permission boundaries as priority changes.

For WSL/vmmem workflows, create a rule for the observed WSL workload process. On the validated Windows 11 machine this was `vmmemWSL.exe`; `vmmemCmZygote` was also visible but should not be targeted unless it is separately validated as the intended workload. Set Base priority to `BelowNormal`, leave Active priority as `Same as normal`, and set `Core Reserve` to the number of physical cores WSL should not use. The reserved cores remain available to Windows and other processes. Use `PriorityGear.Cli core-topology` to inspect physical cores, raw Windows `EfficiencyClass`, reserved masks, and allowed masks. On the validated Intel hybrid machine, `CoreReserve = 1` reserves `0x3` and allows `0xFFFFFFC`; `CoreReserve = 2` reserves `0xF` and allows `0xFFFFFF0`. Installed System Mode validation on `v0.3.6` confirmed that changing only `Core Reserve` from `1` to `2` reapplies the rule to `vmmemWSL.exe`.

If User Mode cannot read or mutate `vmmemWSL.exe` priority/affinity, use System Mode and create an administrator-approved machine rule. Removing a PriorityGear rule does not necessarily restore an already-running process priority or affinity. Normal cleanup is to disable or delete the PriorityGear rule and explicitly restore priority or affinity only when a verified reset path is available. Do not stop WSL, Docker, containers, databases, remote sessions, or other user workloads as routine cleanup.

## v0.3.5 System Mode Installer Release

`v0.3.5` is the latest GitHub release for the formal System Mode installer. It fixes silent uninstall cleanup for package-manager validation after the `v0.3.4` Start Menu installer work.

The GitHub release artifact is:

```text
PriorityGear-v0.3.5-win-x64-installer.zip
```

The zip contains `PriorityGear.Setup.exe`. Double-click it and approve UAC to install or update PriorityGear. The installer is AS IS and unsigned unless signing is explicitly added in a later release.

To build the same installer artifact locally:

```powershell
.\scripts\package-release.ps1 -TagName "v0.3.5" -OutputDirectory ".\artifacts\release-test-v0.3.5"
```

The installer installs the GUI app and configures `PriorityGear.Service` as a LocalSystem Windows Service under a versioned directory below `%ProgramFiles%\PriorityGear\versions`. It preserves `%ProgramData%\PriorityGear\rules.machine.json` and logs under `%ProgramData%\PriorityGear\Logs`.

After install, launch the app from:

```text
Start Menu > PriorityGear > PriorityGear
```

For uninstall, run:

```text
PriorityGear.Setup.exe --uninstall
```

Uninstall stops and deletes the service and removes installed program files. It preserves ProgramData by default.
It also removes the Start Menu shortcut.

The v0.2 verification has confirmed the main service path for an interactive test target, the machine-rule monitor path, a temporary LocalSystem-owned `PriorityGear.TestTarget.Service`, targeted service discovery, and a service-name machine rule for that safe temporary service. After SCM API discovery, the full verification completes in about 8 seconds on the tested Windows 11 machine.

The v0.2 System Mode line contains the first service-side machine-rule monitor. Machine rules live under `%ProgramData%\PriorityGear\rules.machine.json`, are applied only when enabled and administrator-approved, and can be managed through the admin pipe/CLI. It also has SCM-based service-process discovery and service-name rules with shared-host safety gates. Shared-host `svchost.exe` dry-run/reject behavior is verified; arbitrary `svchost.exe` control is not claimed.

v0.2 is in scope for LocalSystem service install/update verification, status/admin named pipes, service-side priority mutation, machine-rule monitoring, service-process discovery, service-name machine rules, CLI administration, and minimal GUI System Mode status visibility.

`v0.2.1` remains the prior public release for System Mode status visibility, but its artifact was still a verification setup zip.

`v0.2.0-preview.1` remains the earlier public prerelease for the System Mode foundation.

v0.2 is out of scope for Store/winget distribution, signing, production MSI/MSIX packaging, GUI machine-rule editing, System Mode active-window priority switching, arbitrary shared-host mutation, CPU affinity, I/O priority, EcoQoS, Realtime priority UI, drivers, telemetry, network features, and updaters.

Post-verification state: `PriorityGear.Service` may remain installed/running, temporary `PriorityGear.TestTarget.Service` must be removed, temporary machine rules are deleted, `%ProgramData%\PriorityGear\Logs` remains, and `%ProgramData%\PriorityGear\rules.machine.json` is preserved or restored. Old version directory cleanup is best-effort.

## Artifacts

### v0.3.5 GitHub Installer

The current GitHub release artifact is:

```text
PriorityGear-v0.3.5-win-x64-installer.zip
```

It contains `PriorityGear.Setup.exe` and the service/app/CLI payload needed for install or update after UAC approval. It is not Store, MSI, MSIX, or signed packaging.

The installer supports `--install --silent` and `--uninstall --silent`.

### winget

winget currently publishes `tsuchim.PriorityGear` version `0.3.4`. GitHub latest is `v0.3.5`, so winget is intentionally behind until a future winget update is prepared and validated. Do not describe winget as unavailable, and do not assume winget has the latest GitHub release until `winget search --id tsuchim.PriorityGear --exact` reports the newer version.

### v0.1 User Mode Portable Publish

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

## Non-Goals for v0.1 User Mode

PriorityGear v0.1 does not include:

- Windows Service.
- Installer, signing, winget, or Microsoft Store packaging.
- System-wide rules.
- `svchost.exe` service-name matching.
- CPU affinity, I/O priority, EcoQoS, or kernel drivers.
- Protected-process bypass.
- Realtime priority in the normal UI.
- Telemetry, network access, updater, or cloud dependencies.

## Security

User Mode only controls processes the current user can control. PriorityGear never bypasses Windows security boundaries and reports failures explicitly when Windows denies access or a process is unsupported.

System Mode requires administrator-approved service installation and runs a LocalSystem Windows Service.

PriorityGear has no telemetry, no network access, and no updater.

## Risk Notice

PriorityGear changes process priority and may affect system responsiveness or stability.
It is provided as-is, without warranty. Use it at your own risk.
System Mode is an administrator feature and may affect system services.
PriorityGear does not bypass Windows security boundaries and does not target protected processes.
System Mode uses separate local named pipes for read-only status and administrator-only mutation. Mutating commands are denied when caller identity cannot be verified.

## License

PriorityGear is released under the MIT License.
