# Manual Test v0.2 System Mode

Run service install and service mutation tests from an elevated PowerShell only where required.

1. Build Release.
2. Install service as administrator.
3. Start service.
4. Confirm service status from GUI or CLI.
5. Confirm service reports whether `SeDebugPrivilege` was enabled.
6. Confirm normal-user mutation through the admin command is rejected.
7. From elevated PowerShell, choose a harmless non-foreground process. Do not launch Notepad or any foreground-stealing app for automated tests.
8. Attempt priority change through service.
9. Confirm success or explicit failure.
10. Choose a harmless process not controllable by User Mode if available.
11. Attempt a protected or denied process only for classification.
12. Confirm explicit unsupported/access-denied result.
13. Stop service.
14. Uninstall service.

Do not start with critical system processes.

Automated or scripted verification must not launch Notepad or any foreground-stealing process. If Notepad is used, start it manually and keep focus behavior under human control.
