# Architecture

PriorityGear is split into App, Core, Windows, and Service boundaries.

## User Mode

User Mode is the v0.1 product. The WPF app runs in the current user session, displays processes and rules, detects the foreground window, and applies priority changes only where Windows grants access.

## System Mode

System Mode is the v0.2 preview boundary. It uses an administrator-approved Windows Service for machine-rule control where Windows permits it. The GUI must not become an unrestricted remote control for a LocalSystem service.

The v0.2 foundation includes a service boundary, machine-rule contracts, named pipe IPC, and explicit Win32 priority APIs. Mutating service commands must be authorized by the service; the GUI and CLI are only clients.

## Projects

- `PriorityGear.App`: WPF UI, tray integration, monitoring loop, user-session foreground polling.
- `PriorityGear.App.Runtime`: monitoring controller, apply decisions, state tracking, and log throttling.
- `PriorityGear.App.Storage`: per-user rule persistence with atomic JSON writes.
- `PriorityGear.App.ViewModels`: process and rule presentation models for WPF.
- `PriorityGear.Core`: rule model, matching, desired-priority calculation, runtime state, structured operation results.
- `PriorityGear.Windows`: thin Windows API wrapper for processes, priorities, foreground window PID, and capability classification.
- `PriorityGear.Service`: LocalSystem-capable worker service for System Mode status, machine-rule monitoring, service discovery, and authorized priority mutation.
- `PriorityGear.Contracts`: neutral IPC and machine-rule contracts shared by the app and service.

## Rule Engine

The User Mode rule engine matches process snapshots against enabled rules. Initial matching supports executable name, full path, and path suffix. Machine rules add administrator-approved service-name matching in System Mode.

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

## IPC Boundary

System Mode IPC uses local named pipes. The status pipe is read-only. The admin pipe is protected for administrator mutation commands. The service validates caller identity, requested scope, rule origin, and requested operation before acting.
