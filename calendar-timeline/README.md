# Command Palette Calendar Timeline

PowerToys Command Palette plugin MVP for a horizontal Outlook calendar timeline.

## Project layout

- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core`: testable timeline, snapshot, privacy and Teams-link logic
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker`: separate worker process; currently supports fake JSON snapshot output
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette`: Command Palette extension skeleton with MSIX/AppExtension metadata for sideload packaging
- `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests`: xUnit tests for core behavior
- `calendar-timeline/docs/superpowers/specs`: design notes and approved spec
- `calendar-timeline/docs/superpowers/plans`: implementation plan

## Local commands

```bash
dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker -- --fake-once
```

Requires .NET 10 SDK in the current development environment.

## Sideload packaging

The Command Palette project now contains `Package.appxmanifest`, COM/AppExtension registration metadata, and placeholder MSIX assets. On Windows, create a sideloadable package with:

```powershell
dotnet publish calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette -f net10.0-windows10.0.26100.0 -c Release -p:Platform=x64
```

The current package starts as a Command Palette extension and exposes a dock band shell backed by the tested snapshot/status model. Real Outlook COM data loading is still pending.
