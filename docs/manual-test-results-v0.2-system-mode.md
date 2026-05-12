# Manual Test Results v0.2 System Mode

Status: partial non-elevated verification only.

## Environment

- User privilege level: normal user (`IsElevated=false`)
- Service install: not run
- Windows Service account: not verified
- Installed service status: not verified

## Non-Elevated Smoke Test

The service host was launched as a hidden normal-user process, not installed as a Windows Service.

- Status pipe: passed
- Status pipe name: `PriorityGear.Service.Status.v0`
- Admin pipe name: `PriorityGear.Service.Admin.v0`
- Status response: `Service running.`
- Service account in smoke test: `tsuchim`
- `SeDebugPrivilege`: attempted, failed with `PrivilegeUnavailable`, Win32 error `1300`
- Non-admin mutation through admin pipe: rejected by pipe access with `Access to the path is denied.`

## Not Run Yet

- Elevated service install.
- LocalSystem account verification.
- Admin-pipe mutation as administrator.
- Service-driven priority change.
- User Mode vs System Mode comparison target.
- Denied/protected process classification.

## Notes

Do not claim System Mode is working until an elevated test installs the service, verifies LocalSystem execution, and changes a safe process priority through the service path.
