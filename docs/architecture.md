# Architecture

PriorityGear is split into App, Core, Windows, and Service boundaries.

## User Mode

User Mode is the v0.1 product. The WPF app runs in the current user session, displays processes and rules, detects the foreground window, and applies priority changes only where Windows grants access.

## System Mode

System Mode is future work. It will use a Windows Service for administrator-approved machine-wide control. The GUI must not become an unrestricted remote control for a LocalSystem service.

The v0.2 foundation introduces a service boundary, machine-rule contracts, named pipe IPC, and explicit Win32 priority APIs. Mutating service commands must be authorized by the service; the GUI is only a client.

## Projects

- `PriorityGear.App`: WPF UI, tray integration, monitoring loop, user-session foreground polling.
- `PriorityGear.App.Runtime`: monitoring controller, apply decisions, state tracking, and log throttling.
- `PriorityGear.App.Storage`: per-user rule persistence with atomic JSON writes.
- `PriorityGear.App.ViewModels`: process and rule presentation models for WPF.
- `PriorityGear.Core`: rule model, matching, desired-priority calculation, runtime state, structured operation results.
- `PriorityGear.Windows`: thin Windows API wrapper for processes, priorities, foreground window PID, and capability classification.
- `PriorityGear.Service`: placeholder boundary for future System Mode.
- `PriorityGear.Contracts`: neutral IPC and machine-rule contracts shared by the app and service.

## Rule Engine

The rule engine matches process snapshots against enabled rules. Initial matching supports executable name, full path, and path suffix. Command-line and service-name matching are reserved fields.

Rules are evaluated in display order, which is creation order in v0.1. The first matching enabled `CurrentUser` rule wins. `Machine` and `ServiceProcess` scopes are reserved and ignored in User Mode.

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

Repeated identical apply failures are throttled by process id, rule id, desired priority, and failure category.

## Future IPC Boundary

System Mode IPC will use an authenticated local boundary such as named pipes. The service must validate caller identity, requested scope, rule origin, and requested operation before acting.
