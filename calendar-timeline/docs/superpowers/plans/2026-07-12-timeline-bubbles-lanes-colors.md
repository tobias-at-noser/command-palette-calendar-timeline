# Timeline Bubbles, Lanes And Colors Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render all readable personal Outlook calendars in a chronological, color-coded snapbar whose bubbles remain readable at narrow widths.

**Architecture:** Extend the shared appointment contract with calendar and ordered category metadata. The worker enriches Outlook records and emits a partial-success status when an individual calendar cannot be loaded. Core owns deterministic lane allocation and privacy sanitization; the Snapbar owns display colors and WPF owns brushes, bubble composition, and lane-one-aligned timeline markers.

**Tech Stack:** .NET 10, C#, xUnit, Outlook COM/MAPI, WPF.

## Global Constraints

- Keep the Command Palette Dock behavior unchanged.
- Preserve the existing time window, IPC contracts, and Teams behavior.
- Treat a private or confidential appointment's categories as sensitive and remove them before projection.
- Use the first Outlook category as the fill color and the calendar color as the bubble border.
- Lane `0` is the visual top lane; additional lanes grow downward.
- The rail is centered in lane `0`; the now-line is symmetric about and limited to lane `0`.
- Resolve stable fallbacks in the UI/Snapbar from calendar identity for every missing or invalid Outlook color; snapshot `CalendarColor` may remain `null`.
- Do not add a calendar selector or color settings UI.

---

### Task 1: Carry Calendar And Category Metadata Safely

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/CalendarCategory.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/Appointment.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/AppointmentSanitizer.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/AppointmentSanitizerTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarSnapshotJsonTests.cs`

**Interfaces:**
- Produces `CalendarCategory(string Name, string? Color)`.
- Extends `Appointment` with optional trailing `CalendarId`, `CalendarName`, `CalendarColor`, and `Categories` fields, preserving all existing construction sites.
- `AppointmentSanitizer.Sanitize(Appointment)` returns protected appointments with `Categories` cleared and calendar identity/color intact.

- [ ] **Step 1: Write failing privacy and JSON round-trip tests**

```csharp
var appointment = new Appointment(
    "private", "Budget", "Room", now, now.AddMinutes(30), true, false, null,
    "calendar-id", "Team", "#4B79A1", [new CalendarCategory("Finance", "#FF0000")]);

var safe = AppointmentSanitizer.Sanitize(appointment);
Assert.Empty(safe.Categories);
Assert.Equal("calendar-id", safe.CalendarId);
Assert.Equal("#4B79A1", safe.CalendarColor);
```

- [ ] **Step 2: Run the focused tests and confirm they fail because the metadata types/properties do not exist**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "FullyQualifiedName~AppointmentSanitizerTests|FullyQualifiedName~CalendarSnapshotJsonTests"`

- [ ] **Step 3: Add the minimal shared records and sanitizer behavior**

```csharp
public sealed record CalendarCategory(string Name, string? Color);

public sealed record Appointment(/* existing fields */, string? TeamsUrl,
    string CalendarId = "", string CalendarName = "", string? CalendarColor = null,
    IReadOnlyList<CalendarCategory>? Categories = null)
{
    public IReadOnlyList<CalendarCategory> Categories { get; init; } = Categories ?? [];
}
```

Set `Categories = []` in the private/confidential `with` expression without modifying calendar fields.

- [ ] **Step 4: Run the focused tests and confirm they pass**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "FullyQualifiedName~AppointmentSanitizerTests|FullyQualifiedName~CalendarSnapshotJsonTests"`
Expected: PASS.

### Task 2: Enrich And Merge Outlook Calendar Results

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentData.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentMapper.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookCalendarSnapshotSource.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Worker/FakeSnapshotFactory.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Host/FakeHostSnapshotSource.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/OutlookAppointmentMapperTests.cs`

**Interfaces:**
- Extends `OutlookAppointmentData` with calendar ID/name/color and ordered `IReadOnlyList<CalendarCategory>`.
- Changes `OutlookAppointmentMapper.CreateSnapshot` to accept optional `statusMessage`.
- `OutlookCalendarSnapshotSource.LoadSnapshotAsync` aggregates readable personal calendar folders and reports `Einige Kalender nicht verfügbar.` if a folder fails after at least one has succeeded.

- [ ] **Step 1: Write failing mapper tests for ordered categories, calendar metadata, stable sorting, and partial-success status**

```csharp
var raw = new OutlookAppointmentData(/* existing values */, "work", "Arbeit", "#3B82B6",
    [new CalendarCategory("Fokus", "#D83B01"), new CalendarCategory("Kunde", "#8764B8")]);
var snapshot = OutlookAppointmentMapper.CreateSnapshot(now, [raw], "Einige Kalender nicht verfügbar.");

var appointment = Assert.Single(snapshot.Appointments);
Assert.Equal("Arbeit", appointment.CalendarName);
Assert.Equal("Fokus", appointment.Categories[0].Name);
Assert.Equal("Einige Kalender nicht verfügbar.", snapshot.StatusMessage);
```

- [ ] **Step 2: Run mapper tests and confirm the new assertions fail**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter FullyQualifiedName~OutlookAppointmentMapperTests`

- [ ] **Step 3: Map records and aggregate individual calendar failures**

Implement the mapper's optional status argument and pass metadata into `Appointment`. In Windows-only Outlook code, enumerate personal calendar folders, sort their appointments by `Start`, preserve Outlook category order, and resolve `NameSpace.Categories[name].Color` through a deterministic Outlook-category-color map. Leave snapshot `CalendarColor` as `null` when Outlook has no usable calendar color; the UI/Snapbar later derives its stable fallback from calendar identity. Catch failures per folder, continue on success, and only throw when no folder produced a result.

- [ ] **Step 4: Update fake data with a calendar plus category fixture and run mapper tests**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter FullyQualifiedName~OutlookAppointmentMapperTests`
Expected: PASS.

### Task 3: Make Core Layout And Snapbar Projection Deterministic

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/TimelineLayout.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBlockViewModel.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineLayoutTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`

**Interfaces:**
- `TimelineLayout.Arrange` orders by `Start`, `End`, then ordinal `Id`; lane `0` is the first and visual top lane.
- `TimelineBlockViewModel` exposes title, short start time, full tooltip, metadata-derived fill/border/foreground colors, lane, ratios, running flag, and Teams URL.

- [ ] **Step 1: Write failing tests for tied starts, lane-zero reuse, ordered tooltip metadata, and short start-time text**

```csharp
var blocks = TimelineLayout.Arrange([sameStartIdB, sameStartIdA]);
Assert.Equal(["a", "b"], blocks.Select(block => block.Appointment.Id));
Assert.Equal("10:00", viewModel.Blocks.Single().StartTime);
Assert.Equal("10:00–10:30 · Room 42 · Arbeit · Fokus, Kunde", viewModel.Blocks.Single().Tooltip);
```

- [ ] **Step 2: Run focused layout, projector, and view-model tests and confirm failure**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "FullyQualifiedName~TimelineLayoutTests|FullyQualifiedName~TimelineVisualProjectorTests|FullyQualifiedName~TimelineSnapbarViewModelTests"`

- [ ] **Step 3: Add exact sort tie-breaker and projection fields**

Use `.ThenBy(appointment => appointment.Id, StringComparer.Ordinal)` in `TimelineLayout`. Build a full tooltip by appending calendar name and comma-separated category names only when present. Carry `CalendarColor` and the first category color to the view model; format `StartTime` with the existing calendar text formatter.

- [ ] **Step 4: Run focused projection tests**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "FullyQualifiedName~TimelineLayoutTests|FullyQualifiedName~TimelineVisualProjectorTests|FullyQualifiedName~TimelineSnapbarViewModelTests"`
Expected: PASS.

### Task 4: Resolve Bubble Colors And Lane-One Geometry

**Files:**
- Create: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBubbleColors.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarLayout.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarLayoutTests.cs`
- Create: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineBubbleColorsTests.cs`

**Interfaces:**
- `TimelineBubbleColors.Resolve(string? categoryColor, string? calendarColor, string calendarIdentity)` returns `Fill`, `Border`, and `Foreground` hexadecimal colors.
- `TimelineSnapbarLayout.GetBlockTop(0, laneCount)` returns `0`; `GetTimelineCenterY()` and `GetNowLineBounds()` describe lane `0` only.

- [ ] **Step 1: Write failing tests for category precedence, fallback stability, contrast, and lane-one-only marker bounds**

```csharp
var colors = TimelineBubbleColors.Resolve("#D83B01", "#3B82B6", "work");
Assert.Equal("#D83B01", colors.Fill);
Assert.Equal("#3B82B6", colors.Border);
Assert.Equal(TimelineSnapbarLayout.BubbleHeight, TimelineSnapbarLayout.GetNowLineBounds().Height);
Assert.Equal(TimelineSnapbarLayout.GetTimelineCenterY(), TimelineSnapbarLayout.GetNowLineBounds().CenterY);
```

- [ ] **Step 2: Run the color and layout tests to confirm failure**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "FullyQualifiedName~TimelineBubbleColorsTests|FullyQualifiedName~TimelineSnapbarLayoutTests"`

- [ ] **Step 3: Implement bounded geometry and pure color resolution**

Increase `BubbleHeight` and lane pitch to preserve padding. Keep lane `0` at top, place the rail at its midpoint, and return a now-line whose top/bottom are symmetric around that midpoint. Normalize `#RRGGBB`, hash nonempty identity into a fixed accessible palette, use category fill before calendar fill, derive a contrasting border for calendar-only events, and select black/white foreground by luminance.

- [ ] **Step 4: Run the color and geometry tests**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter "FullyQualifiedName~TimelineBubbleColorsTests|FullyQualifiedName~TimelineSnapbarLayoutTests"`
Expected: PASS.

### Task 5: Render Two-Line WPF Bubbles And Fixed Markers

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarLayoutTests.cs`

**Interfaces:**
- `CreateBlockButton(TimelineBlockViewModel block, double width)` renders two independently trimmed lines: title, then `StartTime`.
- The WPF rail and now-line consume `TimelineSnapbarLayout` lane-zero geometry.

- [ ] **Step 1: Add failing geometry assertions for minimum width 52 and lane-one-bounded now-line**

```csharp
Assert.Equal(52, TimelineSnapbarLayout.MinimumBlockWidth);
Assert.Equal(TimelineSnapbarLayout.BubbleHeight, TimelineSnapbarLayout.GetNowLineBounds().Height);
```

- [ ] **Step 2: Run the layout tests and confirm failure**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln --filter FullyQualifiedName~TimelineSnapbarLayoutTests`

- [ ] **Step 3: Update WPF layout and bubble composition**

Set rail top and now-line top/height from `TimelineSnapbarLayout`, not total timeline height. Use `Border` with resolved border brush, corner radius, gradient-compatible fill, and running shadow. Render a vertical `StackPanel` with separate title and start-time `TextBlock`s, both `CharacterEllipsis`; use a 52 DIP minimum block width. Assign the full metadata tooltip.

- [ ] **Step 4: Build the WPF project and run the complete suite**

Run: `dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj && dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`
Expected: both commands succeed with zero warnings/errors.

### Task 6: Update Documentation, Review, And Commit

**Files:**
- Modify: `calendar-timeline/README.md`
- Modify: `calendar-timeline/docs/superpowers/specs/2026-07-12-timeline-bubbles-lanes-colors-design.md`

- [ ] **Step 1: Update the README snapbar behavior to mention all personal calendars, color semantics, and lane-one-aligned markers**

- [ ] **Step 2: Reconcile the design spec with implementation names and remove any discrepancy**

- [ ] **Step 3: Run the complete solution test suite**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`
Expected: PASS.

- [ ] **Step 4: Inspect intended changes and commit them**

Run: `git status --short && git diff --check && git diff -- calendar-timeline`

Commit:

```bash
git add calendar-timeline
```

## Plan Self-Review

- Spec coverage: Tasks 1-2 cover all-calendar ingestion, category order/color transport, fallback source data, partial failures, and privacy. Tasks 3-5 cover deterministic lane order, lane-one-aligned rail/now-line, text readability, color rules, and UI rendering. Task 6 preserves documentation and verifies the completed solution.
- Placeholder scan: no deferred work, unspecified tests, or undefined public interfaces remain.
- Type consistency: `CalendarCategory`, optional appointment metadata, `TimelineBubbleColors.Resolve`, and lane-zero geometry are introduced before later consumers.
