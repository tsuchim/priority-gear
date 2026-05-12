# Product Scope

PriorityGear is a Windows 11 process-priority management tool.

## v0.1 User Mode

v0.1 supports:

- WPF GUI application.
- Tray presence.
- Process list display.
- Per-user rules stored as plain JSON.
- Matching by executable name and full path.
- Base priority.
- Active priority when the matched process owns the foreground window.
- Foreground process detection inside the user session.
- Priority application only when the desired priority differs from the last applied priority.
- Periodic process rescan.
- Visible status and logs.

v0.1 explicitly excludes:

- Windows Service.
- Store packaging.
- System-wide rules.
- `svchost.exe` service-name matching.
- Installer.
- Code signing.
- winget.
- Microsoft Store submission.
- Telemetry.
- Network access.
- Updater.
- Kernel driver.
- Protected-process bypass.
- Realtime priority in the normal UI.

## v0.2 Direction

System Mode is planned after User Mode works. It may add:

- Windows Service.
- Machine-wide rules under `%ProgramData%`.
- Administrator-approved rules.
- Named pipe IPC.
- Service-side permission checks.
- `svchost.exe` service-name matching.
- Support for processes outside the current user where Windows permissions allow it.
