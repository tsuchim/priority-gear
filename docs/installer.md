# PriorityGear Installer

PriorityGear `v0.3.1` is the current formal GitHub release installer path. It republishes the installer from the post-adversarial-gate `main` state.

## Artifact

The primary release artifact is:

```text
PriorityGear-v0.3.1-win-x64-installer.zip
```

The zip contains `PriorityGear.Setup.exe` and a `payload` directory with the GUI app, CLI, and System Mode service binaries.

## Install or Update

Double-click `PriorityGear.Setup.exe` and approve UAC. The installer:

- requires elevation;
- installs files under `%ProgramFiles%\PriorityGear\versions\v0.3.1`;
- configures `PriorityGear.Service` as LocalSystem;
- starts or restarts the service;
- confirms the status pipe responds;
- confirms the service reports LocalSystem, process identity, and `SeDebugPrivilege` state;
- writes an install/update log under `%ProgramData%\PriorityGear\Logs`.

The installer fails explicitly if install or update cannot be completed.

## Uninstall

Run:

```powershell
.\PriorityGear.Setup.exe --uninstall
```

Uninstall stops and deletes `PriorityGear.Service`, then removes installed program files under `%ProgramFiles%\PriorityGear`. It preserves `%ProgramData%\PriorityGear` by default, including machine rules and logs.

## Boundaries

The installer is AS IS and unsigned unless signing is implemented in a later release. It is not Store, winget, MSI, or MSIX packaging.

System Mode installs a LocalSystem service and can affect system behavior by changing process priority. PriorityGear does not claim arbitrary shared-host `svchost.exe` mutation.
