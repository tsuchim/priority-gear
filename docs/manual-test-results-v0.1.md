# Manual Test Results v0.1

Status: passed on the local interactive Windows desktop.

## Environment

- Windows version: Microsoft Windows NT 10.0.26200.0
- User privilege level: normal user (`IsElevated=false`)
- App binary used: `src/PriorityGear.App/bin/Release/net10.0-windows/PriorityGear.App.exe`
- Notepad process observed: `notepad.exe`, PID `73780`
- Notepad path: unavailable from the final process object in the scripted verification, but executable-name matching was used

## Results

- Base priority result: `BelowNormal`
- Active priority result: `AboveNormal`
- Return-to-base result: passed, Notepad returned to `BelowNormal` after PriorityGear became foreground
- Persistence result: passed, `%LocalAppData%\PriorityGear\rules.json` existed after rule setup and app run
- Malformed JSON result: passed, `{ malformed` was preserved after app launch and was not silently overwritten
- Known failures: the first attempt using the `Start-Process notepad.exe -PassThru` process object observed a transient launcher process rather than the window-owning Notepad process; the final verification selected the window-owning Notepad process before testing

## Notes

The verification used UI Automation to invoke the PriorityGear **Start** button and Win32 foreground switching to make Notepad and PriorityGear foreground in turn. The app was run without elevation.
