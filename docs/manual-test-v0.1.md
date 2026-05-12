# Manual Test v0.1

This manual test verifies PriorityGear User Mode behavior. It must not require administrator rights.

## Notepad Priority Rule

1. Launch PriorityGear as a normal user.
2. Launch Notepad.
3. Select the Notepad process in the process grid.
4. Click **Add rule**.
5. Set base priority to `BelowNormal`.
6. Set active priority to `AboveNormal`.
7. Ensure active mode is enabled.
8. Click **Start**.
9. Make Notepad the foreground window.
10. Confirm the Notepad row shows desired priority `AboveNormal`.
11. Confirm the last apply result is visible.
12. Switch away from Notepad.
13. Confirm desired priority becomes `BelowNormal`.
14. Confirm the app applies `BelowNormal` and shows the result.
15. Close and restart PriorityGear.
16. Confirm the Notepad rule persists.

## Malformed Rule JSON

1. Stop PriorityGear.
2. Open `%LocalAppData%\PriorityGear\rules.json`.
3. Replace the content with invalid JSON such as `{ malformed`.
4. Launch PriorityGear as a normal user.
5. Confirm the app reports the load failure in the log.
6. Confirm the malformed file is not silently overwritten.

## Failure Visibility

1. Add or enable a rule for a process that Windows denies or no longer exists.
2. Start monitoring.
3. Confirm the process grid or log shows the failure reason.
4. Confirm repeated identical failures do not appear every polling tick.

## Expected Result

The app remains usable without elevation, applies priority only when needed, returns active processes to base priority when they lose foreground, and reports denied or unsupported operations explicitly.
