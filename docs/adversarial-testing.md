# Adversarial Testing

PriorityGear release hardening must prove that broken, hostile, or partial states fail explicitly. A green path test is not enough for installer or System Mode work.

## Policy

- Add adversarial tests before expanding installer, service, pipe, rule, discovery, or release-artifact behavior.
- Do not add fallback success paths. If the primary operation fails, tests should assert failure.
- Prefer admin-free fake executors for installer state-machine behavior. CI must not install or mutate a Windows Service.
- Keep elevated setup verification as local recorded evidence only.
- Treat release artifacts as hostile input: inspect names, checksums, payload shape, and forbidden entries.

## Matrix

| Area | Required adversarial coverage |
| --- | --- |
| Installer state machine | payload missing, service stop/create/start failure, status pipe timeout, non-LocalSystem account, stale binPath, partial copy failure, old-version cleanup warning, no false success |
| Program Files / ProgramData | version path traversal rejection, versioned install directory isolation, ProgramData rules/log preservation, uninstall preserves ProgramData by default |
| Service config | LocalSystem required, service binary must point at the new version directory, stale service configuration cannot be success |
| Named pipe | status pipe rejects mutation/probe commands, oversized request rejection, invalid/partial JSON failure, caller identity unavailable denial, non-admin mutation denial |
| Machine rules | unapproved/disabled/dry-run rules do not mutate, deleted rules and exited processes are pruned, executable-only `svchost.exe` rejected, shared-host mutation gated |
| Service discovery | bounded discovery reports truncation, targeted lookup matches service/PID, missing path/account/priority does not remove valid service results |
| Release artifact | installer zip required, `PriorityGear.Setup.exe` required, service/app/CLI payload required, checksum match required, no `.git`, no `src`, no `tests`, no `*.Tests.dll`, unsafe tag names rejected |
| Uninstall | service stop/delete failure is explicit, Program Files removal failure is explicit, ProgramData is preserved by default, reinstall remains possible |

## Gate For Installer Work

Installer changes must keep these tests passing:

```powershell
dotnet test PriorityGear.slnx --configuration Release --no-build
.\scripts\check-workflow-release-state.ps1
.\scripts\package-release.ps1 -TagName "v0.3.0" -OutputDirectory ".\artifacts\release-test-v0.3.0"
.\scripts\inspect-release-artifacts.ps1 -ArtifactDirectory ".\artifacts\release-test-v0.3.0" -TagName "v0.3.0"
```

The package/inspect commands validate release artifact shape only. They do not run elevated setup.
