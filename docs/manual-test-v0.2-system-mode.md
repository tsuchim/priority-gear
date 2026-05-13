# Manual Test v0.2 System Mode

The primary v0.2 verification path is the local verification setup artifact. Do not use Notepad or any foreground-stealing app for automated verification.

1. Build the verification setup artifact:

   ```powershell
   .\scripts\build-verification-installer.ps1
   ```

2. Double-click:

   ```text
   artifacts\setup-v0.2\PriorityGear-v0.2-system-mode-verification\PriorityGear.VerificationSetup.exe
   ```

3. Approve UAC.
4. Read the final summary window.
5. Open the log file shown by the setup.

Do not start with critical system processes.

The setup installs the service to `%ProgramFiles%\PriorityGear`, starts it as `LocalSystem`, checks the status pipe, checks the administrator mutation pipe, launches `PriorityGear.TestTarget` without foreground focus, changes its priority through the service path, restores it, validates temporary machine rules, and performs a no-mutation probe for denied/protected classification.

The admin pipe protocol reads one bounded request line before caller impersonation. No mutation is executed until the caller is verified as an administrator.

The setup also creates a temporary `PriorityGear.TestTarget.Service` running as LocalSystem, changes and restores that service-owned process priority, verifies targeted service-process discovery and service-name machine rules, checks shared-host dry-run/reject behavior without mutating real shared hosts, then stops and deletes the temporary service. This proves System Mode against safe controlled targets without touching arbitrary Windows services.

The temporary service is created with structured `sc.exe` arguments and a binary path equivalent to:

```text
"<version-dir>\PriorityGear.TestTarget.exe" --service --hold-seconds 120
```

Reruns are expected to be idempotent. The setup first checks for an existing `PriorityGear.Service`, stops it through SCM if it is running, waits for the service process to exit, then installs the new payload into a fresh versioned directory:

```text
%ProgramFiles%\PriorityGear\versions\<timestamp>
```

The service `binPath` is updated to the new version directory. Old version cleanup is best-effort and does not fail verification.

Expected post-verification state:

- `PriorityGear.Service` may remain installed and running.
- `PriorityGear.TestTarget.Service` must not remain installed.
- Temporary machine rules are deleted.
- `%ProgramData%\PriorityGear\Logs` remains.
- `%ProgramData%\PriorityGear\rules.machine.json` is preserved or restored.

If the setup fails, send back the log shown in the summary window. The setup also collects the service log tail from `%ProgramData%\PriorityGear\Logs\service-current.log` when status pipe readiness fails.

Troubleshooting can still use the developer CLI and service scripts, but the normal manual test path is the setup executable above.
