# System Mode Design

System Mode is an administrator-approved extension to User Mode. It exists to control processes that the current user app cannot reach when Windows permits the service to do so.

## Boundary

- The service may run as LocalSystem.
- The GUI is not trusted as an authority.
- Mutating commands require service-side authorization.
- Machine rules are separate from user rules.
- No network listener is allowed.

## Rule Storage

Machine rules are stored at:

```text
%ProgramData%\PriorityGear\rules.machine.json
```

User rules remain at:

```text
%LocalAppData%\PriorityGear\rules.json
```

## IPC

Named pipe IPC is local-only and split by authority:

- `PriorityGear.Service.Status.v0`: read-only status pipe for normal local users.
- `PriorityGear.Service.Admin.v0`: administrator-only mutation pipe protected with an explicit pipe ACL.

The status pipe never executes priority mutation. The admin pipe is the only path for diagnostic and approved machine-rule mutation.

Initial operations:

- `GetServiceStatus`
- `GetMachineRules`
- `TestApplyPriority`
- `ApplyApprovedMachineRule`
- `ProbePriorityAccess`

Normal users may request read-only status. Mutating commands require an administrator caller and, for `ApplyApprovedMachineRule`, an enabled administrator-approved machine rule that matches the target process.

If caller identity cannot be verified, mutation is denied and reported.

`ProbePriorityAccess` checks whether the service can open a process with priority-write access without calling `SetPriorityClass`. It is used for denied/protected classification without mutating critical targets.

## Verification Setup

The v0.2 foundation includes a local verification setup artifact, not a production installer. It is built with:

```powershell
.\scripts\build-verification-installer.ps1
```

The setup executable requests elevation, installs payload files under a versioned directory such as `%ProgramFiles%\PriorityGear\versions\<timestamp>`, registers `PriorityGear.Service` as LocalSystem with `binPath` pointing at that versioned service executable, starts the service, checks status/admin pipes, launches `PriorityGear.TestTarget` without stealing foreground focus, applies and restores priority through the service path, validates temporary machine rules, and writes a detailed log under `%ProgramData%\PriorityGear\Logs`.

This artifact is for local verification only and is not published to GitHub Releases.

Reruns do not delete or overwrite the active base install directory. Old version cleanup is best-effort and non-blocking.

The verification setup includes a safe LocalSystem-owned target check. It temporarily registers `PriorityGear.TestTarget.Service` as LocalSystem, starts it, obtains its service PID, changes and restores priority through the main service admin pipe, then stops and deletes the temporary service. This is intentionally separate from `svchost.exe` service-name matching.

The setup executes `sc.exe` through structured `ProcessStartInfo.ArgumentList` entries rather than one manually quoted command string. This is required for service binary paths containing spaces and additional arguments.

## Machine Rule Runtime

The service includes a conservative machine-rule monitor. It loads `%ProgramData%\PriorityGear\rules.machine.json`, scans processes every 30 seconds, and applies base priority only for enabled administrator-approved rules. Active foreground priority remains a User Mode concept and is not implemented in System Mode.

The monitor records last scan time, rule counts, matched process count, bounded per-rule summaries, and bounded per-process apply results. Status pipe responses include these summaries without dumping unbounded process lists.

Machine rule mutation is available only on the administrator pipe. The service refuses to overwrite malformed machine rule JSON during mutation.

Service process discovery is currently conservative process discovery plus priority-access probing. `svchost.exe` shared-host mutation is not enabled by default and requires later explicit shared-host safety work.

## Service Diagnostics

The service writes an independent file log to:

```text
%ProgramData%\PriorityGear\Logs\service-current.log
```

The log records startup identity, service mode, `SeDebugPrivilege`, pipe server readiness, pipe connections, command kinds, handler responses, write success, disconnects, and exceptions. Verification setup failures include the tail of this log so pipe failures are diagnosable without Event Viewer.

Pipe IPC uses newline-delimited JSON. Each client writes exactly one single-line request and waits for exactly one single-line response. Pretty-printed JSON is not used on the wire because the server reads one request line.

Admin pipe request lines are bounded to 64 KiB. The service reads exactly one bounded request line before calling `RunAsClient` for caller identity because Windows named pipe impersonation may fail before data is read. The request is not executed until authorization succeeds.

## Win32 Priority API

System Mode uses explicit Windows APIs:

- Enable `SeDebugPrivilege` when available.
- Open target process with minimal access.
- Use `PROCESS_SET_INFORMATION` for priority changes.
- Use `PROCESS_QUERY_LIMITED_INFORMATION` for query-only operations.
- Call `SetPriorityClass`.
- Return structured Win32 failures.

`PROCESS_ALL_ACCESS` is not the default.
