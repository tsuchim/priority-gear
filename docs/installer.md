# PriorityGear Installer

PriorityGear `v0.3.2` is the current formal GitHub release installer path. It adds the silent installer mode needed for Windows Package Manager / winget submission.

## Artifact

The primary release artifact is:

```text
PriorityGear-v0.3.2-win-x64-installer.zip
```

The zip contains `PriorityGear.Setup.exe` and a `payload` directory with the GUI app, CLI, and System Mode service binaries.

## Install or Update

Double-click `PriorityGear.Setup.exe` and approve UAC. The installer:

- requires elevation;
- installs files under `%ProgramFiles%\PriorityGear\versions\v0.3.2`;
- configures `PriorityGear.Service` as LocalSystem;
- starts or restarts the service;
- confirms the status pipe responds;
- confirms the service reports LocalSystem, process identity, and `SeDebugPrivilege` state;
- writes an install/update log under `%ProgramData%\PriorityGear\Logs`.

The installer fails explicitly if install or update cannot be completed.

For package-manager automation, use:

```powershell
.\PriorityGear.Setup.exe --install --silent
```

Silent mode runs without setup UI or message boxes after elevation. The required install/update checks are unchanged.

## Uninstall

Run:

```powershell
.\PriorityGear.Setup.exe --uninstall
```

Uninstall stops and deletes `PriorityGear.Service`, then removes installed program files under `%ProgramFiles%\PriorityGear`. It preserves `%ProgramData%\PriorityGear` by default, including machine rules and logs.

For silent uninstall:

```powershell
.\PriorityGear.Setup.exe --uninstall --silent
```

## winget

The installer is submitted for winget distribution as a zip package with nested `PriorityGear.Setup.exe`:

https://github.com/microsoft/winget-pkgs/pull/375643

The winget package is not available until Microsoft validation is complete, the PR is merged, and `winget search` can find it.

## Boundaries

The installer is AS IS and unsigned unless signing is implemented in a later release. It is not Store, MSI, or MSIX packaging.

System Mode installs a LocalSystem service and can affect system behavior by changing process priority. PriorityGear does not claim arbitrary shared-host `svchost.exe` mutation.
