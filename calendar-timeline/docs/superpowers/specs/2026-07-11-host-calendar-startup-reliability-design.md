# Host Calendar Startup Reliability Design

## Goal

The Windows Host must start its tray and Named Pipe endpoint immediately, load Outlook calendar data through a Windows-targeted worker, and recover when the worker does not exit.

## Root Causes

`CalendarTimeline.Host.csproj` currently fixes the worker to `net10.0`. This omits the `WINDOWS` compilation symbol from `OutlookCalendarSnapshotSource`, so every Outlook refresh fails on Windows. In addition, `Program.Main` waits for the first refresh before creating the tray context or starting the pipe server. `WorkerHostSnapshotSource` has no timeout while awaiting the worker process, so a blocked Outlook COM startup prevents the Host from becoming available.

## Design

- Choose the worker target framework from the current Host target framework. Windows Host builds and publishes use `net10.0-windows10.0.19041.0`; non-Windows builds retain `net10.0` for testability.
- Start the Windows tray context and pipe server before scheduling the initial refresh. The first refresh runs in the background and cannot prevent the Host from serving IPC or displaying its tray icon.
- Bound each worker process execution with a timeout. Timeout cancellation terminates the worker process tree and produces a `TimeoutException`.
- Preserve only a safe, user-facing timeout explanation in the Host response. Other worker failures remain the generic availability message.

## Verification

- Unit tests verify target selection, timeout cleanup, timeout status, and ordering of the Host startup sequence.
- Run the solution test suite after restore.
- On Windows, launch the Host with the Windows target and verify the tray icon appears immediately, then launch the WPF app and verify Outlook appointments load or a bounded timeout status is shown.
