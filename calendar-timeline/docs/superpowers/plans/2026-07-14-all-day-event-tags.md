# All-Day Event Tags Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render Outlook all-day appointments as one compact moving tag that shares the timeline fade without consuming a timed-event lane.

**Architecture:** Carry an optional all-day flag from Outlook through Core, project all-day appointments separately from timed blocks, and map one deterministically selected tag into the Snapbar. Put all geometry in a pure layout class, then let WPF draw a non-interactive tag inside the existing masked canvas.

**Tech Stack:** .NET 10, C#, WPF, xUnit v3.

## Global Constraints

- `AllDayEvent` is an optional trailing parameter in public appointment data constructors.
- All-day appointments never create a timed `TimelineBlock` or consume a lane.
- The selected tag is ordered by Start, End, then ordinal ID; every projected all-day title appears in the tooltip and `+N` equals the remaining count.
- The tag parks to the right of the Now line; exit begins exactly at `endX == nowX + gap + tagWidth` and preserves its right-edge continuity.
- The tag shares `BlocksViewport` and `TimelineFadeMask`, is bottom-aligned in lane zero, uses title ellipsis, and has no action.
- Tooltip content is only a structured list of sanitized full titles.
- Preserve normal timed-event layout, fade, current-time indicator, countdown, Teams behavior, and window-height behavior.
- Add production code only after a failing focused test and commit every completed task with a Conventional Commit message containing the design rationale.

---

## File Structure

- `src/CalendarTimeline/src/CalendarTimeline.Core/Appointment.cs`: stores `IsAllDayEvent`.
- `src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentData.cs`, `OutlookAppointmentMapper.cs`, and `OutlookCalendarSnapshotSource.cs`: read and transfer Outlook's flag.
- `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineLayout.cs`: excludes all-day appointments from lanes.
- `src/CalendarTimeline/src/CalendarTimeline.Core/AllDayTimelineVisualTag.cs` and `TimelineVisualProjector.cs`: provide a sanitized, deterministic all-day projection.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/AllDayTagViewModel.cs` and `TimelineSnapbarViewModel.cs`: select and expose one tag plus title-only tooltip entries.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/AllDayTagLayout.cs`: implements deterministic tag geometry.
- `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`: creates the compact non-interactive WPF tag in the existing canvas.
- `tests/CalendarTimeline.Core.Tests/*`: verify data flow, projection, pure geometry, view-model mapping, and WPF source contract.

### Task 1: Preserve All-Day Identity and Exclude Timed Lanes

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Core/Appointment.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentData.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentMapper.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookCalendarSnapshotSource.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineLayout.cs`
- Modify: `tests/CalendarTimeline.Core.Tests/OutlookAppointmentMapperTests.cs`
- Modify: `tests/CalendarTimeline.Core.Tests/OutlookCalendarSnapshotSourceTests.cs`
- Modify: `tests/CalendarTimeline.Core.Tests/TimelineLayoutTests.cs`

**Interfaces:**
- Produces `Appointment.IsAllDayEvent: bool` and `OutlookAppointmentData.IsAllDayEvent: bool`.
- `TimelineLayout.Arrange(IReadOnlyList<Appointment>)` returns only non-all-day `TimelineBlock` values.

- [ ] **Step 1: Write failing mapper and lane tests**

```csharp
Assert.True(snapshot.Appointments[0].IsAllDayEvent);
Assert.Equal(["timed"], TimelineLayout.Arrange([allDay, timed]).Select(block => block.Appointment.Id));
```

- [ ] **Step 2: Run focused tests and verify failure**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~OutlookAppointmentMapperTests|FullyQualifiedName~TimelineLayoutTests"`

Expected: FAIL because `IsAllDayEvent` is absent and all-day appointments still receive lanes.

- [ ] **Step 3: Implement the minimal transfer and filter**

Append `bool IsAllDayEvent = false` to both records, forward it through the mapper, read `Convert.ToBoolean(appointment.AllDayEvent)` when constructing `OutlookAppointmentData`, and begin `Arrange` with:

```csharp
var ordered = appointments
    .Where(appointment => !appointment.IsAllDayEvent)
    .OrderBy(appointment => appointment.Start)
```

- [ ] **Step 4: Add the failing source contract assertion**

```csharp
Assert.Contains("Convert.ToBoolean(appointment.AllDayEvent)", source);
```

- [ ] **Step 5: Run focused and complete tests**

Run: `dotnet test CalendarTimeline.sln --no-restore`

Expected: PASS with the all-day identity retained and lane allocation limited to timed appointments.

- [ ] **Step 6: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Core/Appointment.cs src/CalendarTimeline/src/CalendarTimeline.Core/TimelineLayout.cs src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentData.cs src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookAppointmentMapper.cs src/CalendarTimeline/src/CalendarTimeline.Worker/OutlookCalendarSnapshotSource.cs tests/CalendarTimeline.Core.Tests/OutlookAppointmentMapperTests.cs tests/CalendarTimeline.Core.Tests/OutlookCalendarSnapshotSourceTests.cs tests/CalendarTimeline.Core.Tests/TimelineLayoutTests.cs
```

### Task 2: Project One Displayable All-Day Tag

**Files:**
- Create: `src/CalendarTimeline/src/CalendarTimeline.Core/AllDayTimelineVisualTag.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs`
- Create: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/AllDayTagViewModel.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs`
- Modify: `tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs`
- Modify: `tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`

**Interfaces:**
- Produces `TimelineVisualProjector.ProjectAllDayTags(CalendarSnapshot): IReadOnlyList<AllDayTimelineVisualTag>`.
- `AllDayTimelineVisualTag` contains `Appointment`, `DisplayTitle`, `CalendarColor`, and `CategoryColors`.
- Produces `TimelineSnapbarViewModel.AllDayTag: AllDayTagViewModel?`.
- `AllDayTagViewModel` contains `Title`, `AdditionalCount`, `TooltipTitles`, `CalendarIdentity`, `CalendarColor`, `CategoryColors`, `Start`, and `End`.

- [ ] **Step 1: Write failing Core projection tests**

```csharp
var tags = TimelineVisualProjector.ProjectAllDayTags(snapshot);
Assert.Equal(["a", "b"], tags.Select(tag => tag.Appointment.Id));
Assert.Equal("Privater Termin", tags[1].DisplayTitle);
```

- [ ] **Step 2: Run the focused test and verify failure**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineVisualProjectorTests"`

Expected: FAIL because the all-day projection API and type do not exist.

- [ ] **Step 3: Implement a separate sanitized projection**

Create the record and filter `snapshot.Appointments` by `IsAllDayEvent`, order by Start, End, then ordinal ID, sanitize each appointment, and project its title plus existing color metadata. Keep `Project` unchanged apart from its existing call to the lane-filtered layout.

- [ ] **Step 4: Write failing view-model mapping tests**

```csharp
Assert.Equal("Earlier", viewModel.AllDayTag!.Title);
Assert.Equal(1, viewModel.AllDayTag.AdditionalCount);
Assert.Equal(["Earlier", "Later"], viewModel.AllDayTag.TooltipTitles);
Assert.Single(viewModel.Blocks);
```

- [ ] **Step 5: Run the focused test and verify failure**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineSnapbarViewModelTests"`

Expected: FAIL because the view model does not expose a separately selected all-day tag.

- [ ] **Step 6: Implement one-tag selection and atomic publication**

Map the first all-day projection into `AllDayTagViewModel`, set `AdditionalCount` to `tags.Count - 1`, map all display titles to `TooltipTitles`, and publish `AllDayTag` together with the normal `Blocks` during refresh. The normal timed block ordering remains lane then start ratio.

- [ ] **Step 7: Run focused and complete tests**

Run: `dotnet test CalendarTimeline.sln --no-restore`

Expected: PASS with sanitized title-only tag data and unchanged timed blocks.

- [ ] **Step 8: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Core/AllDayTimelineVisualTag.cs src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs src/CalendarTimeline/src/CalendarTimeline.Snapbar/AllDayTagViewModel.cs src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs
```

### Task 3: Lay Out and Render the Moving Tag

**Files:**
- Create: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/AllDayTagLayout.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarLayout.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Create: `tests/CalendarTimeline.Core.Tests/AllDayTagLayoutTests.cs`
- Modify: `tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- Produces `AllDayTagLayout.GetBounds(double timelineWidth, double nowRatio, double startRatio, double endRatio): (double Left, double Width)`.
- `AllDayTagLayout.TagHeight`, `TagWidth`, and `GapFromNowLine` define stable compact geometry.
- `TimelineSnapbarLayout.GetAllDayTagTop(int laneCount): double` returns the bottom-aligned tag position in lane zero.

- [ ] **Step 1: Write failing pure layout tests**

```csharp
Assert.Equal(startX, AllDayTagLayout.GetBounds(width, nowRatio, startRatio, endRatio).Left);
Assert.Equal(nowX + AllDayTagLayout.GapFromNowLine, parked.Left);
Assert.Equal(endX - AllDayTagLayout.TagWidth, exiting.Left);
Assert.Equal(TimelineSnapbarLayout.BubbleHeight - AllDayTagLayout.TagHeight, TimelineSnapbarLayout.GetAllDayTagTop(3));
```

Include a boundary assertion where `endX == nowX + GapFromNowLine + TagWidth` and the parked and exiting left positions are equal.

- [ ] **Step 2: Run the focused test and verify failure**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~AllDayTagLayoutTests"`

Expected: FAIL because `AllDayTagLayout` and `GetAllDayTagTop` do not exist.

- [ ] **Step 3: Implement the pure geometry**

Use this branch order:

```csharp
if (startX > nowX) return (startX, TagWidth);
if (endX >= parkedRight) return (parkedLeft, TagWidth);
return (endX - TagWidth, TagWidth);
```

Return unclamped coordinates and use `BubbleHeight - TagHeight` for the lane-zero bottom alignment regardless of lane count.

- [ ] **Step 4: Write failing WPF source-structure tests**

Assert that `UpdateLayoutMetrics` calls `CreateAllDayTag`, adds it to `BlocksCanvas`, obtains its bounds from `AllDayTagLayout.GetBounds`, and top-aligns it with `GetAllDayTagTop`. Assert the factory uses `TextTrimming.CharacterEllipsis`, creates a `ToolTip` containing `TooltipTitles`, appends `+{AdditionalCount}`, and does not register `OnBlockClick`.

- [ ] **Step 5: Run the source-structure test and verify failure**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~Task7ReviewFixTests"`

Expected: FAIL because no all-day tag factory exists.

- [ ] **Step 6: Implement masked, non-interactive WPF composition**

During `UpdateLayoutMetrics`, after timed bubbles, conditionally create one `Border` for `viewModel.AllDayTag`. Put it in `BlocksCanvas`, so the existing `BlocksViewport` opacity mask applies. Use the tag layout bounds and `GetAllDayTagTop`; use resolved calendar colors, compact padding, a one-line ellipsized title, and a visible `+N` only when `AdditionalCount > 0`. Build a `ToolTip` stack panel from `TooltipTitles` only. Do not make the border a button and do not attach a click handler.

- [ ] **Step 7: Run complete automated verification**

Run: `dotnet test CalendarTimeline.sln --no-restore && dotnet build src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore`

Expected: PASS with no errors; WPF source tests demonstrate the rendering contract on Linux.

- [ ] **Step 8: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Snapbar/AllDayTagLayout.cs src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarLayout.cs src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs tests/CalendarTimeline.Core.Tests/AllDayTagLayoutTests.cs tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs
```

## Plan Review

- Spec coverage: Task 1 handles Outlook identity and lane exclusion; Task 2 provides deterministic sanitized selection, aggregation, and title-only data; Task 3 implements the exact entry, right-side parking, continuous exit, fade participation, compact geometry, tooltip, and non-interaction contracts.
- Placeholder scan: every task specifies files, interfaces, failing assertions, commands, implementation behavior, and a commit.
- Type consistency: Task 1 adds the flag consumed by Task 2; Task 2 supplies `AllDayTagViewModel` consumed by Task 3; Task 3 owns the layout API exercised by its tests and WPF factory.
