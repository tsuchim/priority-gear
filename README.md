# PriorityGear

PriorityGear is a Windows 11 process-priority manager with foreground-aware rules.
It is inspired by the behavior category of older priority tools, including the Japanese freeware AutoGear, but it is a clean-room implementation.

## v0.1 Scope

PriorityGear v0.1 focuses on User Mode:

- WPF GUI and tray presence.
- Per-user rules stored as plain JSON.
- Matching by executable name and full path.
- Base priority for inactive processes.
- Active priority when a process owns the foreground window.
- Periodic process scanning and foreground polling.
- Visible status, logs, and explicit failures.

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

## License

PriorityGear is released under the MIT License.
