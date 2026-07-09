# Command Palette Calendar Timeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first local/dev MVP foundation for a PowerToys Command Palette calendar timeline plugin backed by a separate Outlook worker.

**Architecture:** Create a focused C# solution under `calendar-timeline/src/CalendarTimeline` with a testable core library, worker console process, and Command Palette plugin project skeleton. Keep Outlook COM isolated in the worker and keep timeline/layout logic independent from UI framework details.

**Tech Stack:** C#/.NET, xUnit tests, System.Text.Json IPC model, Outlook COM/MAPI later in worker, PowerToys Command Palette extension SDK later in plugin project.

## Global Constraints

- Repository currently has no plugin project; create all project files under `calendar-timeline/src/CalendarTimeline`.
- MVP calendar source is local Outlook Desktop via COM/MAPI.
- UI process must not access Outlook COM/MAPI directly.
- Worker provides snapshots for `now - 30 minutes` through `now + 4 hours`.
- Outlook data refresh cadence is one minute.
- Private/confidential appointments are shown as `Privater Termin` with empty location.
- Teams links are detected and exposed as optional appointment data.
- No Settings UI in MVP.
- No Microsoft Graph in MVP.
- No shared calendars in MVP.
- No appointment editing or creation in MVP.
- Commit completed and verified work packages.

---

## File Structure

- `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`: solution for all MVP projects.
- `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/`: framework-independent models, privacy handling, Teams-link detection, time window calculation, overlap layout.
- `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Worker/`: console worker with fake snapshot mode first, Outlook COM adapter later.
- `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.CommandPalette/`: Command Palette plugin skeleton and dock-band integration placeholder.
- `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/`: xUnit tests for core behavior.

---

### Task 1: Scaffold solution and core project

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/CalendarTimeline.Core.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/CalendarWindow.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarWindowTests.cs`

**Interfaces:**
- Produces: `CalendarWindow.Create(DateTimeOffset now)` returning window start/end with -30/+4h offsets.

- [ ] Write failing tests for default window bounds.
- [ ] Implement `CalendarWindow`.
- [ ] Run `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`.

### Task 2: Add appointment snapshot model and privacy sanitization

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/Appointment.cs`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/CalendarSnapshot.cs`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/AppointmentSanitizer.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/AppointmentSanitizerTests.cs`

**Interfaces:**
- Produces: `AppointmentSanitizer.Sanitize(Appointment appointment)`.
- Produces: immutable `CalendarSnapshot` record for IPC and UI.

- [ ] Write tests for private/confidential masking.
- [ ] Implement records and sanitizer.
- [ ] Run core tests.

### Task 3: Add Teams-link detection

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/TeamsLinkDetector.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TeamsLinkDetectorTests.cs`

**Interfaces:**
- Produces: `TeamsLinkDetector.TryFind(string? text)` returning `string?`.

- [ ] Write tests for Teams URL extraction and no-match behavior.
- [ ] Implement detector with simple URL regex for `teams.microsoft.com` and `aka.ms/JoinTeamsMeeting`.
- [ ] Run core tests.

### Task 4: Add overlap lane layout

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/TimelineLayout.cs`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Core/TimelineBlock.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineLayoutTests.cs`

**Interfaces:**
- Produces: `TimelineLayout.Arrange(IReadOnlyList<Appointment> appointments)` returning lane-indexed blocks.

- [ ] Write tests for non-overlapping appointments sharing lane 0.
- [ ] Write tests for overlapping appointments using separate lanes.
- [ ] Implement greedy lane assignment.
- [ ] Run core tests.

### Task 5: Add worker fake snapshot mode

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Worker/CalendarTimeline.Worker.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Worker/Program.cs`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Worker/FakeSnapshotFactory.cs`

**Interfaces:**
- Consumes: `CalendarSnapshot`, `Appointment`, `AppointmentSanitizer`, `TeamsLinkDetector`.
- Produces: JSON snapshot on stdout for UI IPC prototyping.

- [ ] Add worker project reference to core.
- [ ] Emit one fake snapshot as JSON when run with `--fake-once`.
- [ ] Run `dotnet run --project calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.Worker -- --fake-once`.

### Task 6: Add Command Palette plugin skeleton

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.CommandPalette/CalendarTimeline.CommandPalette.csproj`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.CommandPalette/README.md`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.CommandPalette/CalendarTimelineCommandsProvider.cs`
- Create: `calendar-timeline/src/CalendarTimeline/calendar-timeline/src/CalendarTimeline.CommandPalette/CalendarTimelineDockBand.cs`

**Interfaces:**
- Consumes: core snapshot and layout concepts.
- Produces: named project skeleton for later SDK binding.

- [ ] Create skeleton files documenting SDK integration points.
- [ ] Keep project excluded from Linux build if SDK packages are not locally available.
- [ ] Build and test supported projects.

### Task 7: Verify and document next steps

**Files:**
- Modify: `README.md`

**Interfaces:**
- Produces: basic build/test/run instructions.

- [ ] Run `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln` if .NET SDK is available.
- [ ] Run worker fake output command if build succeeds.
- [ ] Update README with current project status and commands.
