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

## Non-Goals for v0.1

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

System Mode is planned for a later milestone and will require administrator-approved service installation.

PriorityGear has no telemetry, no network access, and no updater.

## License

PriorityGear is released under the MIT License.
