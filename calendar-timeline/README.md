# Command Palette Calendar Timeline

PowerToys Command Palette calendar integration with a compact Dock agenda, a separate WPF top snapbar timeline, and a shared Host process that brokers calendar data.

## Architecture overview

- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core`: shared UI-free domain and projection layer. This includes snapshot models plus the shared Core projections used by both clients:
  - `DockAgendaProjector` for compact Command Palette agenda/status rows
  - `TimelineVisualProjector` for the graphical snapbar timeline blocks
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc`: Named Pipe IPC contracts, JSON serialization, pipe naming, client, and server helpers
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host`: long-lived tray/host process that acts as the single data hub, refreshes snapshot state, serves IPC requests, and manages snapbar visibility plus autostart
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette`: PowerToys Command Palette extension that connects to the Host over Named Pipes and renders a compact Dock agenda instead of a freely drawn timeline
- `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf`: always-on-top WPF timeline window that starts at the top of the primary display and can be moved
- `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests`: xUnit coverage for shared Core logic, IPC-adjacent behavior, Host settings/cache behavior, Dock behavior, and snapbar view-model behavior
- `calendar-timeline/docs/superpowers/specs`: design notes and approved specs
- `calendar-timeline/docs/superpowers/plans`: implementation plan

## Runtime model

The Host process is the central calendar data hub. It owns the current `CalendarSnapshot`, refresh flow, tray controls, and local process orchestration.

Both UI surfaces are Host clients:

- the Command Palette Dock requests snapshots through Named Pipe IPC and renders up to three compact agenda/status rows
- the WPF snapbar requests snapshots through the same Named Pipe IPC and renders the full horizontal timeline blocks

Named Pipe messages currently cover ping, get-snapshot, and refresh-snapshot requests, with snapshot/status/error responses. Pipe names are scoped per user.

## UI behavior

### Command Palette Dock

The Dock is intentionally compact. It does not try to draw a custom graphical timeline.

- running meetings appear first
- upcoming meetings follow, with the first upcoming row prefixed as `Als Nächstes`
- subtitle/status text shows snapshot freshness or current status
- if the Host is unavailable or IPC fails, the Dock falls back to `Kalenderdaten nicht verfügbar`
- Teams links are exposed as row actions when available

### WPF top snapbar

The graphical timeline lives in the WPF app, not in the Dock.

- borderless always-on-top window
- starts 400 px wide at the top of the primary display
- can be moved by dragging free timeline space and resized from any edge or corner
- preserves its size and position in `%LocalAppData%/CalendarTimeline/snapbar-window.json`
- grows downward when additional timeline lanes are needed
- renders timeline lanes and meeting blocks from shared Core projections
- shows a semi-transparent milky-white panel with a thin border on hover
- renders a 2 px white now-line with a subtle shadow
- retains Teams click-throughs on appointment blocks
- is shown/hidden by the Host tray menu

## Settings

- Autostart is default **off** (`HostSettings.Default` sets `AutostartEnabled` to `false`)
- Snapbar visibility is default on when the Host launches on Windows
- Current default edge is `Top`

## Local commands

Requires .NET 10 SDK in the current development environment.

```bash
dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host -- --fake-once
dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj
dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj
```

`--fake-once` prints one fake snapshot JSON payload and exits, which is useful for verifying the Host path without starting the tray/IPC loop.

## Sideload packaging

The Command Palette project contains `AppxManifest.xml`, COM/AppExtension registration metadata, and placeholder MSIX assets. On Windows, create a sideloadable package with:

```powershell
dotnet publish calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette -f net10.0-windows10.0.26100.0 -c Release -p:Platform=x64
```

From the publish output directory, register the loose package manifest with:

```powershell
Add-AppxPackage -Register .\AppxManifest.xml
```

The current package starts as a Command Palette extension whose Dock surface is backed by the Host via Named Pipe IPC. The graphical timeline is provided separately by the WPF snapbar application.
