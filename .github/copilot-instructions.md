# Copilot Instructions

PriorityGear is a clean-room Windows process-priority manager.

- Repository documents are English unless explicitly Japanese.
- Do not copy AutoGear code, assets, UI resources, or text.
- Do not add telemetry, network access, updater behavior, kernel drivers, or protected-process bypasses.
- Keep v0.1 focused on User Mode.
- Keep Win32 P/Invoke isolated in `PriorityGear.Windows`.
- Keep core rule behavior testable without Win32 calls.
- Represent failures with structured results.
- Do not simulate success after denied access or unsupported operations.
