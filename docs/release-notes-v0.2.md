# Release Notes v0.2

PriorityGear v0.2 is the first System Mode foundation preview.

## Published Preview

`v0.2.0-preview.1` is published as a public prerelease:

- Release: `https://github.com/tsuchim/priority-gear/releases/tag/v0.2.0-preview.1`
- Artifact: `PriorityGear-v0.2.0-preview.1-system-mode-verification.zip`
- SHA-256: `DCB51E61622E8C84C7CD8B935E10723797DD1C9422C271F8EAF8AE099583D740`
- Tests at release preparation: 64 passed.
- Elevated setup was not run by Codex at publish time.
- Last confirmed elevated verification evidence remains setup version `20260514-004830`.

## Current Release

`v0.2.1` is published as the current public release after `v0.2.0-preview.1`. It exercises the automated release path for plain semantic version tags and polishes operator-facing System Mode status visibility for already-supported service data. It does not expand arbitrary shared-host `svchost.exe` mutation support and is not the final stable `v0.2.0` release.

The `v0.2.1` artifact was still a system-mode verification setup zip. The formal GitHub installer path starts with `v0.3.0`.

## In Scope

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
- Direct service-name discovery and verification retry diagnostics for newly created temporary services.
- Targeted service/PID discovery and truncation metadata for bounded service-process responses.
- Current-scan-only monitor runtime summaries with stale deleted-rule/process entries pruned.
- Shared-host dry-run/reject verification without mutating real shared hosts.
- Faster service discovery using SCM `EnumServicesStatusEx` instead of repeated `sc.exe queryex` calls.
- Minimal GUI System Mode status visibility.
- GUI System Mode status visibility for service binary/version path, configured account, process identity, `SeDebugPrivilege`, machine-rule monitor summary, and service-process discovery truncation metadata.
- Versioned install/update layout for local verification.
- Explicit cleanup expectations for temporary test service and temporary machine rules.

## Out of Scope

- Microsoft Store packaging.
- winget.
- Code signing.
- Polished installer UX.
- Arbitrary `svchost.exe` mutation or shared-host changes without explicit service-name safety gates.
- GUI machine-rule editing.
- System Mode active-window priority switching.
- CPU affinity, I/O priority, or EcoQoS.
- Realtime priority UI.
- Kernel driver.
- Protected-process bypass.
- Network API, telemetry, or updater.
