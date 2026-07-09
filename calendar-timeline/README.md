# Command Palette Calendar Timeline

PowerToys Command Palette plugin MVP for a horizontal Outlook calendar timeline.

## Project layout

- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core`: testable timeline, snapshot, privacy and Teams-link logic
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker`: separate worker process; currently supports fake JSON snapshot output
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette`: Command Palette plugin skeleton for later SDK binding
- `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests`: xUnit tests for core behavior
- `calendar-timeline/docs/superpowers/specs`: design notes and approved spec
- `calendar-timeline/docs/superpowers/plans`: implementation plan

## Local commands

```bash
dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker -- --fake-once
```

Requires .NET 10 SDK in the current development environment.
