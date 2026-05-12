# Roadmap

## v0.1 Prototype

- User Mode WPF app.
- Per-user JSON rules.
- Process list and rule list.
- Base and active priority behavior.
- Foreground detection.
- Visible operation logs.
- Tray presence.
- Rule editing and deletion.
- Visible apply status.
- Duplicate failure throttling.
- Manual test coverage.

## v0.2 System Mode Design and Prototype

- Windows Service prototype.
- Machine-wide rule storage under `%ProgramData%`.
- Named pipe IPC.
- Service-side authorization and validation.
- Service-name matching design for `svchost.exe`.
- Explicit Win32 priority API and privilege result reporting.
- Developer/admin service scripts.

## Later

- v0.1 GitHub Release draft after manual test results are recorded.
- Installer.
- Code signing.
- winget manifest.
- Packaging strategy.
- Accessibility and localization pass.
