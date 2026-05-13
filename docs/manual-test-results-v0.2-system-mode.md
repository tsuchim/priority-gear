# Manual Test Results v0.2 System Mode

Status: verification setup artifact added; elevated setup run still pending.

## First Elevated Verification Attempt

Date: 2026-05-13

The first elevated verification setup run installed and started the LocalSystem service successfully, then failed at the status pipe check.

Observed result:

```text
Service status: Running
Service binary path: C:\Program Files\PriorityGear\PriorityGear.Service.exe
Service account: LocalSystem
STEP: Status pipe
"succeeded": false
"message": "Empty service response."
FAIL: Status pipe failed: Empty service response.
```

Interpretation:

- UAC elevation succeeded.
- Payload installation succeeded.
- Windows Service creation succeeded.
- Service startup succeeded.
- Service account was LocalSystem.
- Failure was in named pipe request/response handling after service startup.

Fix recorded after this attempt:

- Pipe clients now send single-line newline-delimited JSON on the wire.
- Empty/invalid pipe responses are classified explicitly.
- Service-side pipe handlers log each connection, request, response, and exception.
- Status pipe readiness is retried before failure.
- Verification setup includes service log tail diagnostics on failure.

## Second Elevated Verification Attempt

Date: 2026-05-13

The second elevated verification setup run failed during `Install files` because the previously installed service was still running from `%ProgramFiles%\PriorityGear` and locked runtime files.

Observed result:

```text
STEP: Install files
FAIL: System.IO.IOException: The process cannot access the file
'C:\Program Files\PriorityGear\clrjit.dll'
because it is being used by another process.
```

Interpretation:

- The previous service remained installed and running.
- The running service locked installed runtime files such as `clrjit.dll`.
- The setup tried to copy the new payload before stopping the existing service.
- This was an install/update ordering defect, not a new status pipe defect.

Fix recorded after this attempt:

- Verification setup now performs `Existing service cleanup` before `Install files`.
- Existing service state and PID are logged.
- Running service is stopped through SCM before installed files are touched.
- The setup waits for the service to reach `Stopped` and for the service process to exit when the PID is known.
- Install directory cleanup reports locked file/directory paths explicitly.

## Third Elevated Verification Attempt

Date: 2026-05-13

The third elevated verification setup run stopped the existing service successfully, but failed while cleaning the fixed install directory.

Observed result:

```text
STEP: Existing service cleanup
INFO: Existing service found.
INFO: Existing service state: Running
INFO: Existing service PID: 80832
INFO: Stop requested.
INFO: Service stopped.
INFO: Service process already exited.

STEP: Install files
INFO: Cleaning install directory: C:\Program Files\PriorityGear
FAIL: System.IO.IOException: Failed to clean install directory. Locked file: C:\Program Files\PriorityGear\clrjit.dll
 ---> System.UnauthorizedAccessException: Access to the path 'C:\Program Files\PriorityGear\clrjit.dll' is denied.
```

Interpretation:

- Existing service cleanup worked.
- The fixed install directory still contained runtime files that could not be deleted.
- The verification setup should not depend on deleting old runtime files.

Fix recorded after this attempt:

- Verification setup now installs each payload into a new versioned directory under `%ProgramFiles%\PriorityGear\versions\<timestamp>`.
- The service `binPath` is updated to the new versioned `PriorityGear.Service.exe`.
- The base install directory is not cleaned during the normal verification path.
- Old version cleanup is best-effort and non-blocking.

## Fourth Elevated Verification Attempt

Date: 2026-05-13

The fourth elevated verification setup run succeeded through versioned install, service start, status pipe, and `SeDebugPrivilege`, then failed at admin pipe mutation authorization.

Observed result:

```text
Version install directory: C:\Program Files\PriorityGear\versions\20260513-184539
Payload installed to version directory.
Service status: Running
Service binary path: C:\Program Files\PriorityGear\versions\20260513-184539\PriorityGear.Service.exe
Service account: LocalSystem
Status pipe attempt 1: Succeeded=True; Message=Service running.
seDebugPrivilege.succeeded=true

STEP: Admin pipe priority apply
"succeeded": false
"message": "Caller identity unavailable: 名前付きパイプからデータが読み取られるまで、そのパイプを介して偽装することはできません。"
"authorizationSource": "Unavailable"
"commandAllowed": false
```

Interpretation:

- Versioned install and service startup worked.
- Status pipe worked.
- `SeDebugPrivilege` was enabled.
- Admin pipe authorization was attempted before data was read from the pipe.
- Windows named pipe impersonation can fail before request data is read.

Fix recorded after this attempt:

- Admin pipe now reads one bounded request line before caller impersonation.
- Maximum request line size is 64 KiB.
- Admin pipe authorization starts after request read.
- CLI and verification setup use explicit `TokenImpersonationLevel.Impersonation` for admin pipe connections.
- Mutation remains fail-closed if caller identity is still unavailable.

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

- Successful elevated verification setup run.
- Admin-pipe mutation as administrator from the setup.
- Service-driven priority change against `PriorityGear.TestTarget`.
- Machine rule validation from `%ProgramData%\PriorityGear\rules.machine.json`.
- Denied/protected no-mutation probe from the service.

## Verification Setup

The v0.2 verification setup is built with:

```powershell
.\scripts\build-verification-installer.ps1
```

The generated artifact is:

```text
artifacts\setup-v0.2\PriorityGear-v0.2-system-mode-verification\PriorityGear.VerificationSetup.exe
```

Expected user action:

1. Double-click `PriorityGear.VerificationSetup.exe`.
2. Approve UAC.
3. Read the final summary.
4. Send back the log from `%ProgramData%\PriorityGear\Logs\system-mode-verification-*.log`.

## Notes

Do not claim System Mode is working until an elevated test installs the service, verifies LocalSystem execution, and changes a safe process priority through the service path.
