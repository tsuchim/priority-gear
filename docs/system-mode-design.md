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

Named pipe IPC is local-only. Initial operations:

- `GetServiceStatus`
- `GetMachineRules`
- `TestApplyPriority`
- `ApplyApprovedMachineRule`

Normal users may request read-only status. Mutating commands require an administrator caller and administrator-approved machine rules.

## Win32 Priority API

System Mode uses explicit Windows APIs:

- Enable `SeDebugPrivilege` when available.
- Open target process with minimal access.
- Use `PROCESS_SET_INFORMATION` for priority changes.
- Use `PROCESS_QUERY_LIMITED_INFORMATION` for query-only operations.
- Call `SetPriorityClass`.
- Return structured Win32 failures.

`PROCESS_ALL_ACCESS` is not the default.
