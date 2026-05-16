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

## v0.2 System Mode Preview

`v0.2.0-preview.1` is available as a public GitHub prerelease. It is a System Mode preview, not the final stable `v0.2.0` release.

The prerelease includes a local verification setup artifact. This setup is not a production installer.

To build the same verification setup locally:

```powershell
.\scripts\build-verification-installer.ps1
```

Then double-click:

```text
artifacts\setup-v0.2\PriorityGear-v0.2-system-mode-verification\PriorityGear.VerificationSetup.exe
```

Approve UAC. The setup installs a LocalSystem service under `%ProgramFiles%\PriorityGear`, verifies the status pipe and administrator mutation pipe, changes and restores priority for `PriorityGear.TestTarget`, validates temporary machine rules, verifies the machine-rule monitor scan path, checks service-process discovery, and writes a log under `%ProgramData%\PriorityGear\Logs`.

The v0.2 verification has confirmed the main service path for an interactive test target, the machine-rule monitor path, a temporary LocalSystem-owned `PriorityGear.TestTarget.Service`, targeted service discovery, and a service-name machine rule for that safe temporary service. After SCM API discovery, the full verification completes in about 8 seconds on the tested Windows 11 machine.

The v0.2 preview contains the first service-side machine-rule monitor. Machine rules live under `%ProgramData%\PriorityGear\rules.machine.json`, are applied only when enabled and administrator-approved, and can be managed through the admin pipe/CLI. It also has SCM-based service-process discovery and service-name rules with shared-host safety gates. Shared-host `svchost.exe` dry-run/reject behavior is verified; arbitrary `svchost.exe` control is not claimed.

v0.2 is in scope for LocalSystem service install/update verification, status/admin named pipes, service-side priority mutation, machine-rule monitoring, service-process discovery, service-name machine rules, CLI administration, and minimal GUI System Mode status visibility.

v0.2 is out of scope for Store/winget distribution, signing, production MSI/MSIX packaging, GUI machine-rule editing, System Mode active-window priority switching, arbitrary shared-host mutation, CPU affinity, I/O priority, EcoQoS, Realtime priority UI, drivers, telemetry, network features, and updaters.

Post-verification state: `PriorityGear.Service` may remain installed/running, temporary `PriorityGear.TestTarget.Service` must be removed, temporary machine rules are deleted, `%ProgramData%\PriorityGear\Logs` remains, and `%ProgramData%\PriorityGear\rules.machine.json` is preserved or restored. Old version directory cleanup is best-effort.

## Artifacts

### v0.2 Preview Verification Setup

The public prerelease artifact is:

```text
PriorityGear-v0.2.0-preview.1-system-mode-verification.zip
```

It contains `PriorityGear.VerificationSetup.exe` and the service/app/CLI/TestTarget payload needed for local System Mode verification after UAC approval. It is not a production installer.

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
