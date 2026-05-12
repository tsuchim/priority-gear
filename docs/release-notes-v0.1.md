# Release Notes v0.1

PriorityGear v0.1 is the first User Mode release candidate.

## Added

- WPF GUI for Windows 11.
- Tray presence with open/start/stop/exit actions.
- Process grid with PID, executable name, path, current priority, matched rule, desired priority, active state, last applied priority, last result, last apply time, and status.
- Rule grid with add, edit, delete, enable/disable, base priority, active priority, and active mode.
- Per-user JSON rules stored under `%LocalAppData%\PriorityGear\rules.json`.
- Atomic rule save.
- Malformed rule JSON reporting without silent overwrite.
- Foreground-aware active priority switching.
- Monitoring runtime separated from the WPF code-behind.
- Structured priority apply results.
- Duplicate apply-failure throttling.
- Core, runtime, persistence, and Windows wrapper tests.

## Security and Scope

- User Mode only.
- No administrator requirement.
- No Windows Service or System Mode implementation.
- No protected-process bypass.
- No Realtime priority UI.
- No telemetry, network access, or updater.

## Known Limits

- No installer, signing, winget manifest, or Microsoft Store package.
- No service-name matching for `svchost.exe`.
- No CPU affinity, I/O priority, or EcoQoS controls.
- Rule order is creation order in v0.1.
