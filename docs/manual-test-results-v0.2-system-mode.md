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

## Fifth Elevated Verification Attempt

Date: 2026-05-13

Status: passed.

Environment and install:

- Windows: `Microsoft Windows NT 10.0.26200.0`
- User: `HP45L\tsuchim`
- Elevated: `True`
- Version directory: `C:\Program Files\PriorityGear\versions\20260513-185657`
- Service binary path: `C:\Program Files\PriorityGear\versions\20260513-185657\PriorityGear.Service.exe`
- Service account from SCM: `LocalSystem`

Service and authorization:

- Status pipe: succeeded.
- `SeDebugPrivilege`: attempted and succeeded.
- Admin caller: `HP45L\tsuchim`
- Caller SID: `S-1-5-21-472891707-3728080483-1935464772-1001`
- Authorization source: `PipeAcl`
- Admin pipe authorization: succeeded.

Service-path priority mutation:

- TestTarget PID: `24684`
- Original priority: `Normal`
- Priority after apply: `BelowNormal`
- Priority after restore: `Normal`

Machine rule validation:

- Enabled approved matching machine rule: succeeded.
- Disabled rule: rejected.
- Unapproved rule: rejected.
- Executable mismatch: rejected.
- Path mismatch: rejected.

Denied/protected probe:

- PID 4 probe: `AccessDenied`
- Win32 error: `5`
- No mutation attempted.

Final verdict: `passed`.

This is the first confirmed end-to-end System Mode service-path success. It proves service install/update, LocalSystem service startup, status pipe, admin pipe authorization, service-path priority mutation, machine rule validation, and explicit denied/protected probing.

## Remaining Verification Gap

The successful run used a verification process launched by the interactive setup. It does not yet prove priority change for a process owned by LocalSystem or another non-interactive account.

Next verification step:

- Register `PriorityGear.TestTarget.exe` as temporary service `PriorityGear.TestTarget.Service`.
- Run it as LocalSystem.
- Change and restore its priority through `PriorityGear.Service`.
- Stop and delete the temporary service.

## Sixth Elevated Verification Attempt

Date: 2026-05-13

The sixth elevated verification attempt passed the main System Mode mutation and machine rule validation, then failed while creating the temporary LocalSystem TestTarget service.

Observed result:

```text
sc.exe create "PriorityGear.TestTarget.Service" binPath= ""C:\Program Files\PriorityGear\versions\20260513-190732\PriorityGear.TestTarget.exe" --hold-seconds 120" obj= LocalSystem start= demand DisplayName= "PriorityGear TestTarget Service"
```

`sc.exe` returned usage text and exit code `1639`.

Interpretation:

- Main service path still worked.
- LocalSystem TestTarget verification did not start.
- The `sc.exe create` command used malformed nested quoting for `binPath`.

Fix recorded after this attempt:

- Verification setup now uses `ProcessStartInfo.ArgumentList` for `sc.exe` operations.
- TestTarget service `binPath` now uses `"<version-dir>\PriorityGear.TestTarget.exe" --service --hold-seconds 120`.
- `sc.exe` exit code `1639` is summarized as an argument syntax error while preserving full output in the log.
- `PriorityGear.TestTarget` now records explicit service mode when launched with `--service`.

## Seventh Elevated Verification Attempt

Date: 2026-05-13

Status: passed.

Environment and install:

- Windows: `Microsoft Windows NT 10.0.26200.0`
- User: `HP45L\tsuchim`
- Elevated: `True`
- Version: `20260513-191957`
- Version directory: `C:\Program Files\PriorityGear\versions\20260513-191957`
- Service binary path: `C:\Program Files\PriorityGear\versions\20260513-191957\PriorityGear.Service.exe`
- SCM service account: `LocalSystem`

Successful checks:

- Status pipe: succeeded.
- `SeDebugPrivilege`: attempted and succeeded.
- Admin caller: `HP45L\tsuchim`
- Caller SID: `S-1-5-21-472891707-3728080483-1935464772-1001`
- Authorization source: `PipeAcl`
- Normal TestTarget PID `80420`: `Normal -> BelowNormal -> Normal`
- Approved machine rule: succeeded.
- Disabled rule: rejected.
- Unapproved rule: rejected.
- Executable mismatch: rejected.
- Path mismatch: rejected.
- LocalSystem TestTarget service `PriorityGear.TestTarget.Service` PID `84380`: `Normal -> BelowNormal -> Normal`
- LocalSystem TestTarget service stop/delete: succeeded.
- PID 4 probe: `AccessDenied`, Win32 error `5`, no mutation attempted.
- Final verdict: `passed`.

This confirms the System Mode service path for both an interactive verification target and a safe LocalSystem-owned service process. It still does not claim arbitrary Windows service or `svchost.exe` support.

## Machine Rule Runtime Work

After this pass, development moved from verification-only commands toward a real service-side machine-rule monitor:

- service loads `%ProgramData%\PriorityGear\rules.machine.json`
- service scans processes every 30 seconds
- only enabled administrator-approved rules are applied
- disabled/unapproved/malformed rules do not silently become success
- status pipe exposes bounded monitor summaries
- admin pipe exposes rule management, reload, and scan-now commands

## Eighth Elevated Verification Attempt

Date: 2026-05-13

Status: passed.

Environment and install:

- Windows: `Microsoft Windows NT 10.0.26200.0`
- User: `HP45L\tsuchim`
- Elevated: `True`
- Version: `20260513-202550`
- Version directory: `C:\Program Files\PriorityGear\versions\20260513-202550`
- Service binary path: `C:\Program Files\PriorityGear\versions\20260513-202550\PriorityGear.Service.exe`
- SCM configured service account: `LocalSystem`
- Service process identity: `NT AUTHORITY\SYSTEM`

Successful checks:

- Status pipe: succeeded.
- `SeDebugPrivilege`: attempted and succeeded.
- Admin caller: `HP45L\tsuchim`
- Caller SID: `S-1-5-21-472891707-3728080483-1935464772-1001`
- Authorization source: `PipeAcl`
- Normal TestTarget PID `14508`: `Normal -> BelowNormal -> Normal`
- Machine rule monitor:
  - approved rule for `PriorityGear.TestTarget.exe` was added
  - scan completed
  - loaded rule count: `1`
  - enabled approved rule count: `1`
  - matched process count: `1`
  - apply successes: `1`
  - monitor target priority after scan: `BelowNormal`
  - temporary rule deleted after verification
- LocalSystem TestTarget service `PriorityGear.TestTarget.Service` PID `78732`, account `LocalSystem`: `Normal -> BelowNormal -> Normal`
- LocalSystem TestTarget service stop/delete: succeeded.
- PID 4 probe: `AccessDenied`, Win32 error `5`, no mutation attempted.
- Final verdict: `passed`.

This confirms the real monitor scan path for a safe interactive target and confirms direct service-path mutation for a safe LocalSystem-owned service target. It still does not claim arbitrary Windows service or `svchost.exe` priority control.

## Current Development Follow-Up

After this pass, development added guarded service-process discovery and service-name machine rules:

- service discovery groups running services by host PID
- shared service hosts are detected
- service-name rules match only the discovered running service PID
- shared-host rules are rejected unless `allowSharedServiceHost=true`
- executable-only `svchost.exe` rules are rejected by default
- `dryRunOnly=true` reports the target without calling `SetPriorityClass`
- CLI commands expose service-process discovery and service-name rule creation

These changes require another elevated verification setup run before they are treated as proven on the machine.

## Ninth Elevated Verification Attempt

Date: 2026-05-13

Status: failed at service-process discovery verification.

Successful checks before the failure:

- versioned install/update succeeded
- `PriorityGear.Service` started as LocalSystem
- status pipe succeeded
- `SeDebugPrivilege` succeeded
- admin pipe authorization succeeded
- normal TestTarget priority mutation succeeded
- machine rule validation succeeded
- machine rule monitor verification succeeded
- temporary LocalSystem TestTarget service was created and started successfully
- TestTarget service account: `LocalSystem`
- LocalSystem TestTarget PID: `48772`

Failure:

```text
LocalSystem TestTarget service was not discovered with the expected PID.
```

Interpretation:

- This was not a service install/start failure.
- This was not a priority mutation failure.
- The failure was in discovery freshness/completeness or verification timing after the temporary service was created.

Fix direction:

- status pipe discovery now supports direct service-name lookup
- verification setup retries discovery for up to 15 seconds
- each retry records SCM state, SCM PID, direct discovery result, grouped discovery result, and grouped PID contents
- final failure diagnostics include direct SCM path/account and service log tail

## Tenth Elevated Verification Attempt

Date: 2026-05-13

Status: failed at service-process discovery verification.

New observation:

- Temporary LocalSystem TestTarget service was created and running.
- SCM PID was `66800`.
- Direct service-name discovery repeatedly found `PriorityGear.TestTarget.Service` with PID `66800`.
- Direct discovery returned `PriorityGear.TestTarget.exe`, path under `%ProgramFiles%\PriorityGear\versions\20260513-210245`, owner `LocalSystem`, `sharedServiceHost=false`, priority access `Success`, current priority `Normal`.
- Unfiltered grouped discovery returned only 100 groups while status summary reported 172 service host groups.
- The temporary service was omitted from the bounded unfiltered response.

Interpretation:

- Direct service lookup works.
- The bounded unfiltered grouped response is not suitable as the only proof for a specific service.
- Verification must use targeted service/PID discovery and treat the unfiltered grouped response as a bounded summary only.

Fix direction:

- status pipe targeted discovery can return a host group by exact service name or PID
- unfiltered discovery reports `totalDiscoveredGroupCount`, `returnedGroupCount`, `truncated`, and `limit`
- verification accepts direct lookup plus targeted host group lookup
- omission from the bounded unfiltered list no longer fails verification

## Historical Non-Elevated Smoke Test

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

System Mode service-path priority mutation and machine-rule monitor scanning are verified for PriorityGear's safe test targets. Arbitrary Windows service control and `svchost.exe` shared-host mutation are not claimed.
