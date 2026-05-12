# Release Notes v0.2

PriorityGear v0.2 is planned as the first System Mode foundation.

## Planned

- Windows Service host for administrator-approved machine control.
- Machine rules stored separately under `%ProgramData%\PriorityGear\rules.machine.json`.
- Local named pipe IPC between GUI and service.
- Service-side caller and scope validation.
- Explicit Win32 priority API with structured failures.
- `SeDebugPrivilege` enable attempt and visible privilege status.
- Developer/admin service install scripts.

## Not Planned for v0.2

- Microsoft Store packaging.
- winget.
- Code signing.
- Polished installer UX.
- `svchost.exe` service-name matching.
- CPU affinity, I/O priority, or EcoQoS.
- Realtime priority UI.
- Kernel driver.
- Protected-process bypass.
- Network API, telemetry, or updater.
