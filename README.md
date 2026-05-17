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

## v0.3.2 System Mode Installer Release

`v0.3.2` is the current GitHub release for the formal System Mode installer. It adds silent installer switches needed for winget submission. The installer is the normal GitHub-distributed install path for PriorityGear.

The release artifact is:

```text
PriorityGear-v0.3.2-win-x64-installer.zip
```

The zip contains `PriorityGear.Setup.exe`. Double-click it and approve UAC to install or update PriorityGear. The installer is AS IS and unsigned unless signing is explicitly added in a later release.

To build the same installer artifact locally:

```powershell
.\scripts\package-release.ps1 -TagName "v0.3.2" -OutputDirectory ".\artifacts\release-test-v0.3.2"
```

The installer installs the GUI app and configures `PriorityGear.Service` as a LocalSystem Windows Service under a versioned directory below `%ProgramFiles%\PriorityGear\versions`. It preserves `%ProgramData%\PriorityGear\rules.machine.json` and logs under `%ProgramData%\PriorityGear\Logs`.

For uninstall, run:

```text
PriorityGear.Setup.exe --uninstall
```

Uninstall stops and deletes the service and removes installed program files. It preserves ProgramData by default.

The v0.2 verification has confirmed the main service path for an interactive test target, the machine-rule monitor path, a temporary LocalSystem-owned `PriorityGear.TestTarget.Service`, targeted service discovery, and a service-name machine rule for that safe temporary service. After SCM API discovery, the full verification completes in about 8 seconds on the tested Windows 11 machine.

The v0.2 System Mode line contains the first service-side machine-rule monitor. Machine rules live under `%ProgramData%\PriorityGear\rules.machine.json`, are applied only when enabled and administrator-approved, and can be managed through the admin pipe/CLI. It also has SCM-based service-process discovery and service-name rules with shared-host safety gates. Shared-host `svchost.exe` dry-run/reject behavior is verified; arbitrary `svchost.exe` control is not claimed.

v0.2 is in scope for LocalSystem service install/update verification, status/admin named pipes, service-side priority mutation, machine-rule monitoring, service-process discovery, service-name machine rules, CLI administration, and minimal GUI System Mode status visibility.

`v0.2.1` remains the prior public release for System Mode status visibility, but its artifact was still a verification setup zip.

`v0.2.0-preview.1` remains the earlier public prerelease for the System Mode foundation.

v0.2 is out of scope for Store/winget distribution, signing, production MSI/MSIX packaging, GUI machine-rule editing, System Mode active-window priority switching, arbitrary shared-host mutation, CPU affinity, I/O priority, EcoQoS, Realtime priority UI, drivers, telemetry, network features, and updaters.

Post-verification state: `PriorityGear.Service` may remain installed/running, temporary `PriorityGear.TestTarget.Service` must be removed, temporary machine rules are deleted, `%ProgramData%\PriorityGear\Logs` remains, and `%ProgramData%\PriorityGear\rules.machine.json` is preserved or restored. Old version directory cleanup is best-effort.

## Artifacts

### v0.3.2 GitHub Installer

The current GitHub release artifact is:

```text
PriorityGear-v0.3.2-win-x64-installer.zip
```

It contains `PriorityGear.Setup.exe` and the service/app/CLI payload needed for install or update after UAC approval. It is not Store, MSI, MSIX, or signed packaging.

For winget submission, the installer supports `--install --silent` and `--uninstall --silent`. The winget package is not available until the `microsoft/winget-pkgs` PR is validated and merged.

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
