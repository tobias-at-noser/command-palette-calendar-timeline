# Command Palette Calendar Timeline

PowerToys Command Palette plugin MVP for a horizontal Outlook calendar timeline.

## Project layout

- `src/CalendarTimeline/src/CalendarTimeline.Core`: testable timeline, snapshot, privacy and Teams-link logic
- `src/CalendarTimeline/src/CalendarTimeline.Worker`: separate worker process; currently supports fake JSON snapshot output
- `src/CalendarTimeline/src/CalendarTimeline.CommandPalette`: Command Palette plugin skeleton for later SDK binding
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests`: xUnit tests for core behavior
- `docs/superpowers/specs`: design notes and approved spec
- `docs/superpowers/plans`: implementation plan

## Local commands

```bash
dotnet test src/CalendarTimeline/CalendarTimeline.sln
dotnet run --project src/CalendarTimeline/src/CalendarTimeline.Worker -- --fake-once
```

Requires .NET 10 SDK in the current development environment.
