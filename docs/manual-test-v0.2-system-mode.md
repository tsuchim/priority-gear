# Manual Test v0.2 System Mode

Run service install and service mutation tests from an elevated PowerShell only where required.

1. Build Release.
2. Install service as administrator.
3. Start service.
4. Confirm service status from GUI or CLI.
5. Confirm service reports whether `SeDebugPrivilege` was enabled.
6. Choose a harmless process not controllable by User Mode if available.
7. Attempt priority change through service.
8. Confirm success or explicit failure.
9. Attempt a protected or denied process.
10. Confirm explicit unsupported/access-denied result.
11. Stop service.
12. Uninstall service.

Do not start with critical system processes.
