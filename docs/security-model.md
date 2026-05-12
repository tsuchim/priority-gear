# Security Model

PriorityGear does not bypass Windows security boundaries.

## User Mode

User Mode only controls processes that the current user can control through documented Windows APIs. When access is denied or a process is unsupported, PriorityGear records and displays the failure.

Missing executable path is not treated as proof that priority cannot be changed. A process may still match by executable name and may still be controllable.

## System Mode

System Mode is future work and requires administrator-approved Windows Service installation.

The GUI must not become an unrestricted remote control for a LocalSystem service. A future service must validate caller identity, requested rule scope, target process scope, and authorization before applying any change.

System Mode service execution may use LocalSystem. LocalSystem has strong local machine privileges, so the service boundary is a security boundary:

- The service must validate caller identity.
- Named pipe IPC separates read-only status from administrator-only mutation.
- The service must distinguish read-only status requests from mutating requests.
- The service must validate requested rule scope.
- Machine rules must be administrator-approved before execution.
- Access denied is a valid result and must be reported explicitly.
- Protected processes are reported as unsupported.
- Realtime priority remains hidden unless explicitly designed later.

The v0.2 verification setup is a local administrator tool for validating this boundary. It requests UAC, installs the service as LocalSystem under `%ProgramFiles%\PriorityGear`, uses a harmless `PriorityGear.TestTarget` process for mutation verification, and uses a no-mutation probe for denied/protected classification.

## Protected Processes

Protected processes are not targets. PriorityGear must report protected or unsupported cases explicitly.

## Realtime Priority

Realtime priority is intentionally hidden from the normal UI because it can make a system unstable.

## Failure Handling

Failures are explicit:

- Do not mark a failed priority change as applied.
- Do not substitute another priority.
- Do not silently retry aggressively.
- Do not report success when the main operation failed.

PriorityGear v0.1 has no telemetry, no network access, and no updater.
