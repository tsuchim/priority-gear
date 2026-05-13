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
- Verification setup reruns stop an existing service before updating service registration.
- Verification setup uses versioned install directories under `%ProgramFiles%\PriorityGear\versions` so old runtime files do not block updates.
- Admin pipe authorization now runs after one bounded request line is read, matching Windows named pipe impersonation requirements.
- Verification setup now includes a temporary LocalSystem-owned `PriorityGear.TestTarget.Service` priority mutation check.
- Verification setup uses structured `sc.exe` arguments for service create/config/delete commands to avoid nested quote failures.
- Service-side machine-rule monitor with 30-second scanning for enabled administrator-approved rules.
- Admin pipe machine-rule management commands: add, update, enable, disable, approve, unapprove, delete, reload, and scan-now.
- Status pipe machine-rule monitor summary.
- Conservative service-process discovery and priority-access probe without default `svchost.exe` mutation.
- Service-name machine rules with shared-host safety gates.
- Dry-run machine rules for discovery and target validation without mutation.
- CLI service-process discovery commands.

## Not Planned for v0.2

- Microsoft Store packaging.
- winget.
- Code signing.
- Polished installer UX.
- Arbitrary `svchost.exe` mutation or shared-host changes without explicit service-name safety gates.
- CPU affinity, I/O priority, or EcoQoS.
- Realtime priority UI.
- Kernel driver.
- Protected-process bypass.
- Network API, telemetry, or updater.
