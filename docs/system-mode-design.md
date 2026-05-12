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

The setup executable requests elevation, installs payload files under `%ProgramFiles%\PriorityGear`, registers `PriorityGear.Service` as LocalSystem, starts the service, checks status/admin pipes, launches `PriorityGear.TestTarget` without stealing foreground focus, applies and restores priority through the service path, validates temporary machine rules, and writes a detailed log under `%ProgramData%\PriorityGear\Logs`.

This artifact is for local verification only and is not published to GitHub Releases.

## Win32 Priority API

System Mode uses explicit Windows APIs:

- Enable `SeDebugPrivilege` when available.
- Open target process with minimal access.
- Use `PROCESS_SET_INFORMATION` for priority changes.
- Use `PROCESS_QUERY_LIMITED_INFORMATION` for query-only operations.
- Call `SetPriorityClass`.
- Return structured Win32 failures.

`PROCESS_ALL_ACCESS` is not the default.
