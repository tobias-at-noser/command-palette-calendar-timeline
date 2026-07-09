# CalendarTimeline.CommandPalette

Skeleton for the PowerToys Command Palette extension.

The current project keeps SDK binding thin while the core timeline and worker contracts are developed. The final extension should replace the placeholder provider/dock-band classes with `Microsoft.CommandPalette.Extensions` types, expose the dock band via `GetDockBands()`, and render the timeline from `CalendarSnapshot` data.
