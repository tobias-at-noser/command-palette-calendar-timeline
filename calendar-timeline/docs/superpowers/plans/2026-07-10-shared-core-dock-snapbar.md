# Shared Core Dock Snapbar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor Calendar Timeline into a shared-core architecture with a compact Command Palette Dock agenda and a separate WPF top-screen timeline snapbar backed by a local tray/host data hub.

**Architecture:** Keep `CalendarTimeline.Core` UI-free and add shared projections for Dock and Timeline. Add a `CalendarTimeline.Ipc` project for Named Pipe contracts, a `CalendarTimeline.Host` tray/data-hub process, and a `CalendarTimeline.Wpf` snapbar client. Convert `CalendarTimeline.CommandPalette` from direct worker process usage to a thin Named Pipe client that renders only API-supported dock rows.

**Tech Stack:** C#/.NET 10, xUnit, System.Text.Json, System.IO.Pipes, WPF for the snapbar, Windows Forms NotifyIcon or equivalent tray support for the host, PowerToys Command Palette extension SDK for dock integration.

## Global Constraints

- `CalendarTimeline.Core` must not depend on WPF, PowerToys SDK, Named Pipes, Outlook COM, tray APIs, or Windows UI packages.
- The Command Palette Dock must not attempt free custom timeline rendering; it may use only icon, title, subtitle, and command/list item affordances.
- The graphical timeline belongs in the WPF snapbar, not the Dock.
- The Host is the single long-lived calendar data hub.
- Dock and WPF clients communicate with Host through Named Pipes.
- Autostart is optional and disabled by default.
- The first snapbar position is top edge of the primary screen.
- WPF snapbar MVP supports display, hover details, and Teams/Outlook click targets.
- Existing fake-data and test paths must remain usable without real Outlook.
- Private/confidential appointments remain anonymized before any UI displays them.
- Microsoft Graph, Windows Widgets, Taskbar embedding, appointment editing, and full settings UI are out of scope.
- Run `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln` after implementation tasks when available.

---

## File Structure

- Modify: `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`
  - Add `CalendarTimeline.Ipc`, `CalendarTimeline.Host`, and `CalendarTimeline.Wpf` projects.
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/`
  - Add UI-independent projection and formatting types used by both Dock and WPF.
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/`
  - Named Pipe protocol records, serializer, pipe name provider, client, server loop.
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/`
  - Long-lived local host/tray process, snapshot cache, fake data source wiring, IPC server, basic tray commands.
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette/`
  - Replace direct worker process client with IPC client and compact Dock agenda projection.
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/`
  - WPF top snapbar, ViewModel, timeline drawing surface, hover/click behavior.
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/`
  - Add tests for projection, formatting, IPC protocol serialization, host fake provider, CommandPalette failure behavior, and WPF ViewModel where buildable in the environment.
- Modify: `calendar-timeline/README.md`
  - Update architecture, commands, and current limitations.
- Modify: `calendar-timeline/docs/superpowers/specs/2026-07-09-commandpalette-calendar-plugin-design.md`
  - Mark the old Dock timeline assumption as superseded by the new shared-core/snapbar design.

---

### Task 1: Supersede old Dock timeline spec and document new architecture

**Files:**
- Modify: `calendar-timeline/docs/superpowers/specs/2026-07-09-commandpalette-calendar-plugin-design.md`
- Verify: `calendar-timeline/docs/superpowers/specs/2026-07-10-shared-core-dock-snapbar-design.md`

**Interfaces:**
- Consumes: verified PowerToys Dock limitation: Dock supports command/list item surfaces, not arbitrary timeline drawing.
- Produces: documentation baseline for all implementation agents.

- [ ] **Step 1: Update old spec header**

  Insert this note directly below the H1 in `2026-07-09-commandpalette-calendar-plugin-design.md`:

  ```markdown
  > Superseded note: The original assumption that the PowerToys Command Palette Dock can render a custom horizontal timeline is no longer valid for implementation. The Dock implementation is now a compact agenda/status band. The graphical timeline is implemented separately as a WPF snapbar. See `2026-07-10-shared-core-dock-snapbar-design.md`.
  ```

- [ ] **Step 2: Replace the old Dock UI section**

  Replace the section beginning with `## Dock UI` through the paragraph ending `gestapelten Überschneidungen.` with:

  ```markdown
  ## Dock UI

  Die Dock-Ansicht ist keine frei gerenderte horizontale Timeline. Die Command Palette Dock API wird im MVP als kompakte Agenda-/Status-Band genutzt.

  Kernelemente:

  - maximal 1 bis 3 sichtbare Agenda-Zeilen
  - Icon, Titel, Untertitel und Command pro Zeile
  - laufender Termin wird priorisiert
  - nächster Termin wird als zweite Priorität angezeigt
  - Status-/Fehlerzeile wird dezent angezeigt
  - Teams-Link wird als Command geöffnet, wenn vorhanden

  Die grafische horizontale Timeline wird in der separaten WPF-Snapbar umgesetzt.
  ```

- [ ] **Step 3: Self-review documentation consistency**

  Search the old spec for `kontinuierlich animierte Dock-Timeline`, `Timeline kontinuierlich animieren`, and `horizontale Timeline`. Rewrite each occurrence so it either refers to the WPF snapbar or the compact Dock agenda.

- [ ] **Step 4: Verify docs contain no obsolete requirement**

  Run:

  ```bash
  rg "frei gerenderte|kontinuierlich animierte Dock|Dock-Ansicht ist eine horizontale Timeline" calendar-timeline/docs/superpowers/specs
  ```

  Expected: no matches that still require a graphical Dock timeline.

- [ ] **Step 5: Commit**

  ```bash
  git add calendar-timeline/docs/superpowers/specs/2026-07-09-commandpalette-calendar-plugin-design.md calendar-timeline/docs/superpowers/specs/2026-07-10-shared-core-dock-snapbar-design.md
  git commit -m "docs: revise calendar timeline architecture"
  ```

---

### Task 2: Add shared Core presentation projections

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/DockAgendaItem.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/DockAgendaProjector.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualBlock.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/CalendarTextFormatter.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/DockAgendaProjectorTests.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs`

**Interfaces:**
- Consumes: `CalendarSnapshot`, `Appointment`, `TimelineLayout.Arrange(IReadOnlyList<Appointment>)`.
- Produces: `DockAgendaProjector.Project(CalendarSnapshot snapshot, int maxItems)` returning `IReadOnlyList<DockAgendaItem>`.
- Produces: `TimelineVisualProjector.Project(CalendarSnapshot snapshot)` returning `IReadOnlyList<TimelineVisualBlock>`.

- [ ] **Step 1: Write Dock projection tests**

  Add tests covering:

  ```csharp
  [Fact]
  public void Project_PrioritizesRunningAppointment()
  {
      var now = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
      var snapshot = new CalendarSnapshot(
          now,
          now.AddMinutes(-30),
          now.AddHours(4),
          [
              new Appointment("next", "Planning", "Room", now.AddMinutes(30), now.AddMinutes(60), false, false, null),
              new Appointment("running", "Daily", "Teams", now.AddMinutes(-10), now.AddMinutes(20), false, false, "https://teams.microsoft.com/l/meetup-join/example")
          ],
          null);

      var items = DockAgendaProjector.Project(snapshot, 3);

      Assert.Equal("Jetzt · Daily", items[0].Title);
      Assert.Contains("20 Min. verbleibend", items[0].Subtitle);
      Assert.True(items[0].IsRunning);
      Assert.NotNull(items[0].TeamsUrl);
  }

  [Fact]
  public void Project_AddsNextAppointmentAfterRunningAppointment()
  {
      var now = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
      var snapshot = new CalendarSnapshot(
          now,
          now.AddMinutes(-30),
          now.AddHours(4),
          [
              new Appointment("running", "Daily", "Teams", now.AddMinutes(-10), now.AddMinutes(20), false, false, null),
              new Appointment("next", "Planning", "Room", now.AddMinutes(30), now.AddMinutes(60), false, false, null)
          ],
          null);

      var items = DockAgendaProjector.Project(snapshot, 3);

      Assert.Equal("Als Nächstes · Planning", items[1].Title);
      Assert.Equal("09:30–10:00 · Room", items[1].Subtitle);
  }
  ```

- [ ] **Step 2: Write Timeline projection tests**

  Add tests covering normalized positions:

  ```csharp
  [Fact]
  public void Project_ComputesNormalizedStartAndEndWithinWindow()
  {
      var now = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
      var snapshot = new CalendarSnapshot(
          now,
          now.AddMinutes(-30),
          now.AddMinutes(90),
          [new Appointment("a", "Focus", "", now, now.AddMinutes(30), false, false, null)],
          null);

      var blocks = TimelineVisualProjector.Project(snapshot);

      Assert.Single(blocks);
      Assert.Equal(0.25, blocks[0].StartRatio, 3);
      Assert.Equal(0.5, blocks[0].EndRatio, 3);
      Assert.True(blocks[0].IsRunning);
  }
  ```

- [ ] **Step 3: Run tests to verify failure**

  Run:

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "DockAgendaProjectorTests|TimelineVisualProjectorTests"
  ```

  Expected: fail because new types do not exist.

- [ ] **Step 4: Implement records and formatter**

  Implement:

  ```csharp
  namespace CalendarTimeline.Core;

  public sealed record DockAgendaItem(
      string Title,
      string Subtitle,
      int Lane,
      bool IsRunning,
      string? TeamsUrl,
      string? AppointmentId);
  ```

  ```csharp
  namespace CalendarTimeline.Core;

  public sealed record TimelineVisualBlock(
      Appointment Appointment,
      int Lane,
      double StartRatio,
      double EndRatio,
      bool IsRunning,
      string DisplayTitle,
      string DisplaySubtitle);
  ```

  ```csharp
  namespace CalendarTimeline.Core;

  public static class CalendarTextFormatter
  {
      public static string FormatTimeRange(DateTimeOffset start, DateTimeOffset end) => $"{start:HH:mm}–{end:HH:mm}";

      public static string FormatDuration(TimeSpan duration)
      {
          var minutes = Math.Max(0, (int)Math.Ceiling(duration.TotalMinutes));
          if (minutes < 60)
          {
              return $"{minutes} Min.";
          }

          var hours = minutes / 60;
          var remainingMinutes = minutes % 60;
          return remainingMinutes == 0 ? $"{hours} Std." : $"{hours} Std. {remainingMinutes} Min.";
      }
  }
  ```

- [ ] **Step 5: Implement DockAgendaProjector and TimelineVisualProjector**

  Implement running appointments first, next future appointments second, status fallback last. Clamp timeline visual ratios to `[0, 1]`.

- [ ] **Step 6: Run full tests**

  Run:

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

  Expected: pass.

- [ ] **Step 7: Commit**

  ```bash
  git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests
  git commit -m "feat: add shared calendar presentation projections"
  ```

---

### Task 3: Add Named Pipe IPC contracts and client/server

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimeline.Ipc.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimelinePipeNames.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimelineRequest.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimelineResponse.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimelinePipeJson.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimelinePipeClient.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc/CalendarTimelinePipeServer.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimelinePipeJsonTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`

**Interfaces:**
- Consumes: `CalendarSnapshot` from Core.
- Produces: `CalendarTimelinePipeClient.SendAsync(CalendarTimelineRequest request, CancellationToken cancellationToken)`.
- Produces: `CalendarTimelinePipeServer.RunAsync(Func<CalendarTimelineRequest, CancellationToken, Task<CalendarTimelineResponse>> handler, CancellationToken cancellationToken)`.

- [ ] **Step 1: Create failing serialization tests**

  ```csharp
  [Fact]
  public void SerializeAndDeserializeSnapshotResponse_RoundTripsSnapshot()
  {
      var now = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
      CalendarTimelineResponse response = new SnapshotResponse(
          new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4), [], "ok"));

      var json = CalendarTimelinePipeJson.SerializeResponse(response);
      var roundTrip = CalendarTimelinePipeJson.DeserializeResponse(json);

      var snapshot = Assert.IsType<SnapshotResponse>(roundTrip);
      Assert.Equal("ok", snapshot.Snapshot.StatusMessage);
  }
  ```

- [ ] **Step 2: Add project and solution entry**

  Create `CalendarTimeline.Ipc.csproj` targeting `net10.0`, with project reference to Core, nullable enabled, implicit usings enabled, warnings as errors.

- [ ] **Step 3: Define request and response records**

  Required records:

  ```csharp
  public abstract record CalendarTimelineRequest(string Type);
  public sealed record PingRequest() : CalendarTimelineRequest("ping");
  public sealed record GetSnapshotRequest() : CalendarTimelineRequest("getSnapshot");
  public sealed record RefreshSnapshotRequest() : CalendarTimelineRequest("refreshSnapshot");
  ```

  ```csharp
  public abstract record CalendarTimelineResponse(string Type);
  public sealed record StatusResponse(string Status) : CalendarTimelineResponse("status");
  public sealed record SnapshotResponse(CalendarSnapshot Snapshot) : CalendarTimelineResponse("snapshot");
  public sealed record ErrorResponse(string Message) : CalendarTimelineResponse("error");
  ```

- [ ] **Step 4: Implement JSON serialization**

  Use `System.Text.Json` with explicit type discriminator handling. The tests must not require reflection over UI types.

- [ ] **Step 5: Implement pipe names**

  `CalendarTimelinePipeNames.Default` returns a stable per-user name using `Environment.UserName`, sanitized to letters, digits, dot, underscore, and dash.

- [ ] **Step 6: Implement client/server**

  Use `NamedPipeClientStream` and `NamedPipeServerStream` in message-per-line JSON form. Each client request opens a connection, writes one JSON line, reads one JSON line, and closes.

- [ ] **Step 7: Run tests**

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter CalendarTimelinePipeJsonTests
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

- [ ] **Step 8: Commit**

  ```bash
  git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Ipc calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  git commit -m "feat: add calendar timeline named pipe ipc"
  ```

---

### Task 4: Add Host data hub with fake snapshot mode and tray shell

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/CalendarTimeline.Host.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/Program.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/CalendarTimelineHostService.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/HostSnapshotCache.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/FakeHostSnapshotSource.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/TrayApplicationContext.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/HostSnapshotCacheTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`

**Interfaces:**
- Consumes: Core models and `CalendarTimeline.Ipc` server.
- Produces: long-lived host process responding to `Ping`, `GetSnapshot`, and `RefreshSnapshot`.

- [ ] **Step 1: Write HostSnapshotCache tests**

  ```csharp
  [Fact]
  public void SnapshotCache_ReturnsStatusWhenNoSnapshotExists()
  {
      var cache = new HostSnapshotCache();

      var response = cache.GetSnapshotResponse();

      var error = Assert.IsType<ErrorResponse>(response);
      Assert.Equal("Kalenderdaten nicht verfügbar", error.Message);
  }
  ```

- [ ] **Step 2: Create Host project**

  Target `net10.0-windows10.0.19041.0` on Windows and `net10.0` elsewhere if needed for testability. Reference Core and Ipc. Enable Windows targeting, nullable, implicit usings, warnings as errors.

- [ ] **Step 3: Implement fake snapshot source**

  Reuse the fake appointment shape from the existing Worker fake path. Include one running appointment and one next appointment.

- [ ] **Step 4: Implement HostSnapshotCache**

  It stores the latest `CalendarSnapshot?` and status. It returns `SnapshotResponse` when a snapshot exists and `ErrorResponse("Kalenderdaten nicht verfügbar")` when not.

- [ ] **Step 5: Implement Host service request handler**

  Handler behavior:

  - `PingRequest` -> `StatusResponse("ok")`
  - `GetSnapshotRequest` -> cache response
  - `RefreshSnapshotRequest` -> refresh fake source first, update cache, return snapshot response

- [ ] **Step 6: Add tray shell**

  On Windows, use a minimal tray application context with menu entries:

  - `Timeline anzeigen`
  - `Timeline verbergen`
  - `Jetzt aktualisieren`
  - `Autostart aktivieren`
  - `Beenden`

  The first MVP can wire show/hide to no-op methods until the WPF process exists, but the menu labels must exist.

- [ ] **Step 7: Add command-line fake host mode**

  `dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host -- --fake-once` prints one snapshot JSON via existing `CalendarSnapshotJson` or `System.Text.Json` and exits.

- [ ] **Step 8: Run tests and smoke command**

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter HostSnapshotCacheTests
  dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host -- --fake-once
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

- [ ] **Step 9: Commit**

  ```bash
  git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  git commit -m "feat: add calendar timeline host data hub"
  ```

---

### Task 5: Convert Command Palette Dock to IPC agenda client

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette/IHostSnapshotClient.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette/PipeHostSnapshotClient.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette/CalendarTimeline.CommandPalette.csproj`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette/CalendarTimelineDockBand.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette/CalendarTimelineCommandsProvider.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimelineDockBandTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimelineCommandsProviderTests.cs`

**Interfaces:**
- Consumes: `DockAgendaProjector.Project`, `CalendarTimelinePipeClient`.
- Produces: Dock band rows from Named Pipe snapshot data.

- [ ] **Step 1: Update tests for compact Dock rows**

  Adjust or add tests so the Dock band asserts:

  - no graphical timeline concepts are exposed
  - rows come from `DockAgendaProjector`
  - max visible rows is 3
  - pipe errors become `Kalenderdaten nicht verfügbar`

- [ ] **Step 2: Add IHostSnapshotClient**

  ```csharp
  namespace CalendarTimeline.CommandPalette;

  using CalendarTimeline.Core;

  public interface IHostSnapshotClient
  {
      Task<CalendarSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
  }
  ```

- [ ] **Step 3: Implement PipeHostSnapshotClient**

  Use `CalendarTimelinePipeClient` to send `GetSnapshotRequest`. Return snapshot for `SnapshotResponse`. Throw `InvalidOperationException` with the response message for `ErrorResponse`. Throw for unexpected response types.

- [ ] **Step 4: Update project references**

  Add project reference from CommandPalette to `CalendarTimeline.Ipc`.

- [ ] **Step 5: Refactor CalendarTimelineDockBand**

  Replace private row-building logic with `DockAgendaProjector.Project(snapshot, 3)`. Remove duplicated duration formatter if Core now owns it.

- [ ] **Step 6: Refactor provider client wiring**

  Replace `WorkerProcessSnapshotClient` as the default data client with `PipeHostSnapshotClient`. Keep existing worker client only if tests still need it, but do not use it as default production path.

- [ ] **Step 7: Run targeted tests**

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "CalendarTimelineDockBandTests|CalendarTimelineCommandsProviderTests"
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

- [ ] **Step 8: Commit**

  ```bash
  git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.CommandPalette calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests
  git commit -m "feat: connect command palette dock to host ipc"
  ```

---

### Task 6: Add WPF snapbar ViewModel and window shell

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/App.xaml`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/App.xaml.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/TimelineSnapbarViewModel.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/TimelineBlockViewModel.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`

**Interfaces:**
- Consumes: `CalendarTimelinePipeClient`, `TimelineVisualProjector.Project`.
- Produces: top-edge Always-on-top WPF timeline window backed by ViewModel blocks.

- [ ] **Step 1: Write ViewModel tests**

  Add tests that instantiate `TimelineSnapbarViewModel` with a fake snapshot provider and assert:

  - running appointment appears as highlighted block
  - status text is set when IPC fails
  - blocks are sorted by lane then start ratio

- [ ] **Step 2: Create WPF project**

  Use `Microsoft.NET.Sdk`, `UseWPF=true`, target Windows TFM, reference Core and Ipc.

- [ ] **Step 3: Implement ViewModel**

  `TimelineSnapbarViewModel` exposes:

  - `ObservableCollection<TimelineBlockViewModel> Blocks`
  - `string StatusText`
  - `Task RefreshAsync(CancellationToken cancellationToken)`

  `TimelineBlockViewModel` exposes title, subtitle, lane, start ratio, width ratio, running flag, and Teams URL.

- [ ] **Step 4: Implement top snapbar window shell**

  In `MainWindow.xaml.cs`, set:

  - `Topmost = true`
  - `WindowStyle = None`
  - `ResizeMode = NoResize`
  - `Left = 0`
  - `Top = 0`
  - `Width = SystemParameters.PrimaryScreenWidth`
  - compact fixed height such as `96`

- [ ] **Step 5: Implement basic timeline rendering**

  Use WPF layout primitives sufficient for MVP:

  - horizontal container
  - fixed now line in center or at ratio corresponding to generated time
  - appointment blocks positioned by start/width ratios
  - lane-based vertical offsets
  - tooltip or hover details using WPF `ToolTip`

- [ ] **Step 6: Implement Teams click**

  If block has Teams URL, clicking opens via `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`.

- [ ] **Step 7: Run tests/build**

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter TimelineSnapbarViewModelTests
  dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

- [ ] **Step 8: Commit**

  ```bash
  git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  git commit -m "feat: add wpf calendar timeline snapbar"
  ```

---

### Task 7: Wire Host tray commands to WPF snapbar and autostart setting

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/HostSettings.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/HostSettingsStore.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/SnapbarProcessController.cs`
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/AutostartManager.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/TrayApplicationContext.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/HostSettingsStoreTests.cs`

**Interfaces:**
- Consumes: WPF executable path and Host tray menu.
- Produces: show/hide snapbar commands and optional disabled-by-default autostart setting.

- [ ] **Step 1: Write settings tests**

  Test default settings:

  ```csharp
  [Fact]
  public void HostSettings_Defaults_DisableAutostart()
  {
      var settings = HostSettings.Default;

      Assert.False(settings.AutostartEnabled);
      Assert.True(settings.ShowSnapbar);
      Assert.Equal("Top", settings.Edge);
  }
  ```

- [ ] **Step 2: Implement HostSettings and store**

  Store JSON under `%LOCALAPPDATA%/CalendarTimeline/settings.json` on Windows and a temp/test override path in tests.

- [ ] **Step 3: Implement SnapbarProcessController**

  Responsibilities:

  - `ShowAsync()` starts `CalendarTimeline.Wpf.exe` if not running
  - `HideAsync()` closes or kills the tracked process for MVP
  - no crash if executable is missing; return a status message instead

- [ ] **Step 4: Implement AutostartManager**

  Use current-user startup folder shortcut or registry Run key. Default remains disabled. Provide methods:

  - `IsEnabled()`
  - `SetEnabled(bool enabled)`

- [ ] **Step 5: Wire tray menu**

  Connect tray menu commands:

  - `Timeline anzeigen` -> `SnapbarProcessController.ShowAsync()`
  - `Timeline verbergen` -> `SnapbarProcessController.HideAsync()`
  - `Autostart aktivieren/deaktivieren` -> toggles setting and manager
  - `Jetzt aktualisieren` -> host refresh
  - `Beenden` -> shutdown

- [ ] **Step 6: Run tests**

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter HostSettingsStoreTests
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

- [ ] **Step 7: Commit**

  ```bash
  git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests
  git commit -m "feat: wire host tray controls"
  ```

---

### Task 8: Update README and verify full solution

**Files:**
- Modify: `calendar-timeline/README.md`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: accurate developer instructions for the shared-core Dock/Snapbar architecture.

- [ ] **Step 1: Update project layout**

  README layout must mention:

  - `CalendarTimeline.Core`
  - `CalendarTimeline.Ipc`
  - `CalendarTimeline.Host`
  - `CalendarTimeline.CommandPalette`
  - `CalendarTimeline.Wpf`
  - tests

- [ ] **Step 2: Update local commands**

  Include:

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host -- --fake-once
  dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf
  ```

- [ ] **Step 3: Document current UX split**

  State clearly:

  - Command Palette Dock is a compact agenda/status band.
  - WPF Snapbar is the graphical timeline.
  - Host is the data hub.
  - Autostart is optional and disabled by default.

- [ ] **Step 4: Run verification**

  ```bash
  dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  dotnet build calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
  ```

- [ ] **Step 5: Commit**

  ```bash
  git add calendar-timeline/README.md
  git commit -m "docs: document shared core dock snapbar workflow"
  ```

---

## Subagent Execution Strategy

Use `superpowers:subagent-driven-development` for implementation.

Recommended dispatch order:

1. Task 1 documentation correction
2. Task 2 Core projections
3. Task 3 IPC
4. Task 4 Host
5. Task 5 CommandPalette conversion
6. Task 6 WPF snapbar
7. Task 7 Host tray wiring
8. Task 8 README and final verification

Each subagent receives exactly one task section plus the Global Constraints. After each subagent returns, review `git diff`, run the task's verification command, and only then dispatch the next task.

## Self-Review

Spec coverage:

- Shared Core: Task 2
- Named Pipe IPC: Task 3
- Host/Tray process: Task 4 and Task 7
- Compact Dock agenda: Task 5
- WPF top snapbar: Task 6
- Optional autostart disabled by default: Task 7
- Documentation migration: Task 1 and Task 8
- Verification: every task includes targeted tests and final full test/build pass

Placeholder scan: no task depends on undefined future work for its main deliverable. Tray show/hide no-op is allowed only before Task 6 exists; Task 7 wires it to the WPF process.

Type consistency: Core projection types from Task 2 are consumed by CommandPalette and WPF tasks; IPC request/response names from Task 3 are consumed by Host and CommandPalette tasks.
