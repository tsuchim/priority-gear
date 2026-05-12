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

Troubleshooting can still use the developer CLI and service scripts, but the normal manual test path is the setup executable above.
