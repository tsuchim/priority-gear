# PriorityGear Installer

PriorityGear `v0.3.4` is the next formal GitHub release installer path. It follows the Windows desktop convention that installed applications are launched from the Start Menu.

## Artifact

The primary release artifact is:

```text
PriorityGear-v0.3.4-win-x64-installer.zip
```

The zip contains `PriorityGear.Setup.exe` and a `payload` directory with the GUI app, CLI, and System Mode service binaries.

## Install or Update

Double-click `PriorityGear.Setup.exe` and approve UAC. The installer:

- requires elevation;
- installs files under `%ProgramFiles%\PriorityGear\versions\v0.3.4`;
- configures `PriorityGear.Service` as LocalSystem;
- starts or restarts the service;
- confirms the status pipe responds;
- confirms the service reports LocalSystem, process identity, and `SeDebugPrivilege` state;
- registers `PriorityGear` in the machine-wide Start Menu;
- writes an install/update log under `%ProgramData%\PriorityGear\Logs`.

The installer fails explicitly if install or update cannot be completed.

For package-manager automation, use:

```powershell
.\PriorityGear.Setup.exe --install --silent
```

Silent mode runs without setup UI or message boxes after elevation. The required install/update checks are unchanged.

## Launch

After install, launch PriorityGear from:

```text
Start Menu > PriorityGear > PriorityGear
```

The Start Menu shortcut opens the installed GUI app from the current version directory under `%ProgramFiles%\PriorityGear\versions`.

## Uninstall

Run:

```powershell
.\PriorityGear.Setup.exe --uninstall
```

Uninstall stops and deletes `PriorityGear.Service`, then removes installed program files under `%ProgramFiles%\PriorityGear`. It preserves `%ProgramData%\PriorityGear` by default, including machine rules and logs.
It also removes the Start Menu shortcut.

For silent uninstall:

```powershell
.\PriorityGear.Setup.exe --uninstall --silent
```

## winget

winget registration is not done in this release. A previous submission was closed because the installed application launch path and documentation were not ready. The package must not be treated as available until a future winget PR is validated, merged, and `winget search` can find it.

## Boundaries

The installer is AS IS and unsigned unless signing is implemented in a later release. It is not Store, MSI, or MSIX packaging.

System Mode installs a LocalSystem service and can affect system behavior by changing process priority. PriorityGear does not claim arbitrary shared-host `svchost.exe` mutation.
