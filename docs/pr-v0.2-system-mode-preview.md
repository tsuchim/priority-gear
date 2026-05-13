# Summary

- v0.2 System Mode preview for PriorityGear.
- Adds the LocalSystem service foundation used for privileged, administrator-approved priority changes.
- Adds machine-rule storage and the service-side machine-rule monitor.
- Adds service-process discovery and service-name machine rules.
- Adds shared-host safety gates so shared service hosts are rejected or dry-run by default instead of silently mutated.

# Verified

Latest elevated verification setup result:

- Setup version: `20260514-004830`
- Final verdict: `passed`
- Total duration: about 8 seconds
- Direct service discovery: 7 ms
- Targeted service host discovery: 2 ms
- Bounded service discovery summary: 130 ms
- Shared-host bounded discovery: 131 ms
- Shared-host post-scan targeted discovery: 3 ms
- PID 4 probe: `AccessDenied`, Win32 error `5`

The elevated verification covered:

- LocalSystem service install/update using a versioned install directory
- status/admin named pipe split
- admin pipe authorization
- `SeDebugPrivilege` enable attempt
- direct service-path priority mutation
- machine-rule monitor scan path
- LocalSystem-owned TestTarget service mutation
- service-name machine rule matching
- shared-host dry-run/reject safety
- cleanup of temporary machine rules and temporary TestTarget service

# Safety Boundary

PriorityGear System Mode is privileged software. It installs a LocalSystem Windows Service and changes process priority, which can affect system responsiveness or stability. It is provided AS IS and should be used at the user's own risk.

Shared-host service processes are guarded. Arbitrary `svchost.exe` mutation is not claimed in this preview, executable-only `svchost.exe` rules are not treated as safe, and shared-host mutation is not enabled by default.

# Out Of Scope

- arbitrary shared-host `svchost.exe` priority mutation
- GUI machine-rule editing
- Microsoft Store distribution
- winget distribution
- code signing
- production-grade MSI/MSIX installer
- CPU affinity
- I/O priority
- EcoQoS
- Realtime priority UI
- driver-level support
- telemetry, updater, or network features
- System Mode active-window priority switching

# Validation

Commands run before opening this PR:

```powershell
dotnet restore PriorityGear.slnx
dotnet build PriorityGear.slnx --configuration Release --no-restore
dotnet test PriorityGear.slnx --configuration Release --no-build
.\scripts\build-verification-installer.ps1
```

Result:

```text
Build succeeded
Tests passed: 62
Verification setup artifact generated
Elevated setup was not run by Codex
```
