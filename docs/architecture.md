# Architecture

PriorityGear is split into App, Core, Windows, and Service boundaries.

## User Mode

User Mode is the v0.1 product. The WPF app runs in the current user session, displays processes and rules, detects the foreground window, and applies priority changes only where Windows grants access.

## System Mode

System Mode is future work. It will use a Windows Service for administrator-approved machine-wide control. The GUI must not become an unrestricted remote control for a LocalSystem service.

## Projects

- `PriorityGear.App`: WPF UI, tray integration, monitoring loop, user-session foreground polling.
- `PriorityGear.Core`: rule model, matching, desired-priority calculation, runtime state, structured operation results.
- `PriorityGear.Windows`: thin Windows API wrapper for processes, priorities, foreground window PID, and capability classification.
- `PriorityGear.Service`: placeholder boundary for future System Mode.

## Rule Engine

The rule engine matches process snapshots against enabled rules. Initial matching supports executable name, full path, and path suffix. Command-line and service-name matching are reserved fields.

## Active Priority Engine

If active mode is enabled and the process owns the current foreground window, the desired priority is the rule's active priority. Otherwise it is the base priority.

The core calculation is independent of Win32 APIs. Foreground detection belongs to the user-session app through the Windows wrapper.

## Permission Classification

Process capability is represented explicitly:

- Controllable now.
- Current user only.
- Administrator required.
- Service mode required.
- Protected or unsupported.
- Unknown error.

v0.1 applies changes only to controllable current-user processes.

## Logging

Runtime operations produce structured results with status, error details, and timestamps. The UI shows failures rather than treating them as success.

## Future IPC Boundary

System Mode IPC will use an authenticated local boundary such as named pipes. The service must validate caller identity, requested scope, rule origin, and requested operation before acting.
