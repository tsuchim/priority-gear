# Release Notes v0.2

PriorityGear v0.2 is planned as the first System Mode foundation.

## Planned

- Windows Service host for administrator-approved machine control.
- Machine rules stored separately under `%ProgramData%\PriorityGear\rules.machine.json`.
- Local named pipe IPC between GUI and service.
- Separate status and administrator-only mutation pipes.
- Service-side caller and scope validation.
- Explicit Win32 priority API with structured failures.
- `SeDebugPrivilege` enable attempt and visible privilege status.
- Developer/admin service install scripts.
- Developer CLI diagnostics for service status and admin-pipe test commands.
- Local verification setup artifact that performs the elevated service install and System Mode checks after UAC approval.
- `PriorityGear.TestTarget`, a harmless non-foreground test process for service-path priority mutation.
- No-mutation priority access probe for denied/protected classification.
- Service-side file logging for pipe diagnostics under `%ProgramData%\PriorityGear\Logs`.
- Hardened newline-delimited JSON pipe protocol with explicit empty/invalid response classification.
- Status pipe readiness retry in the verification setup.

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
