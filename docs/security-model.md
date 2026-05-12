# Security Model

PriorityGear does not bypass Windows security boundaries.

## User Mode

User Mode only controls processes that the current user can control through documented Windows APIs. When access is denied or a process is unsupported, PriorityGear records and displays the failure.

## System Mode

System Mode is future work and requires administrator-approved Windows Service installation.

The GUI must not become an unrestricted remote control for a LocalSystem service. A future service must validate caller identity, requested rule scope, target process scope, and authorization before applying any change.

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
