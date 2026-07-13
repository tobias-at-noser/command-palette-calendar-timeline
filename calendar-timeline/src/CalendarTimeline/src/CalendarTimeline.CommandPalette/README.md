# CalendarTimeline.CommandPalette

PowerToys Command Palette extension for the Calendar Timeline project.

The extension is now a thin client over the shared architecture:

- it connects to `CalendarTimeline.Host` through `CalendarTimeline.Ipc`
- it renders compact agenda/status rows produced by `CalendarTimeline.Core.DockAgendaProjector`
- it falls back to `Kalenderdaten nicht verfügbar` when the Host cannot be reached
- it does not render the graphical timeline directly; that surface lives in `CalendarTimeline.Wpf`

Use the repository README for end-to-end architecture and verification commands.
