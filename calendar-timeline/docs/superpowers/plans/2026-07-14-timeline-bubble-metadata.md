# Timeline Bubble Metadata Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render every timeline event as a compact two-line bubble with an always-visible title prefix, an optional duration, and a structured tooltip containing the full title.

**Architecture:** Keep display-ready duration and tooltip context in the existing Core-to-Snapbar projection so WPF does not parse event text. Let the WPF view build the two visual rows and use one pure Snapbar layout rule to decide whether the duration fits; the tooltip is a native WPF visual tree built from the existing view-model fields.

**Tech Stack:** .NET 10, C#, WPF, xUnit v3.

## Global Constraints

- Preserve `TimelineSnapbarLayout.BubbleHeight = 32` and `MinimumBlockWidth = 52`.
- The upper row always shows the start time; it never shows an end time.
- Show duration only at or above a documented width threshold; hide it below that threshold.
- Render the lower title row at every width with `CharacterEllipsis`, so its first characters remain visible even at `52 px`.
- Center the dot between start time and duration on the upper row.
- The structured tooltip presents the full title first, then time range plus duration, then the existing location/calendar/category context when present.
- Keep title sanitization and calendar-color behavior unchanged.
- Add only production code preceded by a failing test, and commit every completed task with a Conventional Commit message.

---

## File Structure

- `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualBlock.cs`: adds display-only duration and tooltip-context fields to the projection record.
- `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs`: creates the new strings from appointment data without duplicating context in the tooltip.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBlockViewModel.cs`: exposes duration and tooltip context to WPF.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs`: transfers the new projected values into the bubble view model.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBubbleLayout.cs`: owns the deterministic duration-visibility threshold.
- `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`: composes the upper and lower bubble rows and the structured native tooltip.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs`: covers projection of duration and tooltip context.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`: covers transfer of duration and tooltip context.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineBubbleLayoutTests.cs`: covers both sides of the duration-width threshold.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`: verifies the WPF source retains the required visual structure where WPF cannot run on Linux.
- `docs/design-previews/event-label-layout-final.svg` and `.png`: retain the approved 32-pixel visual reference.

### Task 1: Project Bubble Metadata

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualBlock.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBlockViewModel.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`

**Interfaces:**
- Produces `TimelineVisualBlock.DisplayDuration: string` and `TimelineVisualBlock.TooltipContext: string`.
- Produces `TimelineBlockViewModel.Duration: string` and `TimelineBlockViewModel.TooltipContext: string`.
- `TooltipContext` contains location, calendar, and categories joined by ` · `, but does not include the time range.

- [ ] **Step 1: Write failing Core projection tests**

```csharp
Assert.Equal("30 Min.", block.DisplayDuration);
Assert.Equal("Room 42 · Arbeit · Fokus, Kunde", block.TooltipContext);
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineVisualProjectorTests"`

Expected: FAIL because `DisplayDuration` and `TooltipContext` do not exist.

- [ ] **Step 3: Add minimal projection fields and formatting**

```csharp
CalendarTextFormatter.FormatDuration(appointment.End - appointment.Start)
```

Build tooltip context from non-empty location, calendar name, and category names; retain `DisplaySubtitle` unchanged for existing consumers.

- [ ] **Step 4: Write failing Snapbar transfer assertions**

```csharp
Assert.Equal("30 Min.", block.Duration);
Assert.Equal("Room 42 · Arbeit · Fokus, Kunde", block.TooltipContext);
```

- [ ] **Step 5: Run focused tests to verify the transfer test fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineSnapbarViewModelTests"`

Expected: FAIL because the view model has no duration/context properties.

- [ ] **Step 6: Add view-model fields and transfer them unchanged**

```csharp
public string Duration { get; }
public string TooltipContext { get; }
```

Pass the projected values through `TimelineSnapbarViewModel.RefreshAsync` when constructing `TimelineBlockViewModel`.

- [ ] **Step 7: Run focused and full tests**

Run: `dotnet test CalendarTimeline.sln --no-restore`

Expected: PASS with all tests green.

- [ ] **Step 8: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualBlock.cs src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBlockViewModel.cs src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs
git commit -m "feat(timeline): project duration and tooltip context" -m "Keep WPF presentation declarative by supplying independently formatted duration and non-time context from the existing projection pipeline."
```

### Task 2: Render Compact Bubble and Tooltip

**Files:**
- Create: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBubbleLayout.cs`
- Create: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineBubbleLayoutTests.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`
- Add: `docs/design-previews/event-label-layout-final.svg`
- Add: `docs/design-previews/event-label-layout-final.png`

**Interfaces:**
- Produces `TimelineBubbleLayout.ShouldShowDuration(double width): bool`.
- WPF consumes `TimelineBlockViewModel.StartTime`, `.Duration`, `.Title`, and `.TooltipContext`.

- [ ] **Step 1: Write the failing layout tests**

```csharp
Assert.False(TimelineBubbleLayout.ShouldShowDuration(TimelineSnapbarLayout.MinimumBlockWidth));
Assert.True(TimelineBubbleLayout.ShouldShowDuration(TimelineBubbleLayout.DurationVisibleMinimumWidth));
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineBubbleLayoutTests"`

Expected: FAIL because `TimelineBubbleLayout` does not exist.

- [ ] **Step 3: Implement the minimal pure layout rule**

```csharp
public const double DurationVisibleMinimumWidth = 142;

public static bool ShouldShowDuration(double width)
{
    return width >= DurationVisibleMinimumWidth;
}
```

- [ ] **Step 4: Run the focused layout tests**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineBubbleLayoutTests"`

Expected: PASS.

- [ ] **Step 5: Write failing WPF source-structure tests**

Assert that the source creates two `TextBlock` rows, uses `TextTrimming.CharacterEllipsis` on the title row, calls `TimelineBubbleLayout.ShouldShowDuration(width)`, creates a `ToolTip` with the title, and includes a center-aligned dot element.

- [ ] **Step 6: Run the source-structure test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~Task7ReviewFixTests"`

Expected: FAIL because the old one-line `DockPanel` and plain text tooltip are still present.

- [ ] **Step 7: Implement the WPF composition**

Replace `CreateBubbleLabel` with a two-row grid. Its top row contains start time, a `TextBlock` dot with vertical centering, and duration whose `Visibility` follows `TimelineBubbleLayout.ShouldShowDuration(width)`. Its second row is a no-wrap `TextBlock` using `CharacterEllipsis`, preserving the leading title characters. Replace the current plain `TextBlock` tooltip with a `ToolTip` holding title, `StartTime–EndTime · Duration`, and non-empty `TooltipContext`; preserve the `480` maximum width.

- [ ] **Step 8: Run the full test suite and compile the Windows project**

Run: `dotnet test CalendarTimeline.sln --no-restore && dotnet build src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore`

Expected: PASS with no warnings or errors.

- [ ] **Step 9: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBubbleLayout.cs src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineBubbleLayoutTests.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs docs/design-previews/event-label-layout-final.svg docs/design-previews/event-label-layout-final.png
git commit -m "feat(timeline): render compact event metadata" -m "Reserve the lower line for a title prefix at every supported width while making duration a width-dependent enhancement and promoting full event detail into a readable tooltip."
```

## Plan Review

- Spec coverage: Task 1 supplies duration and context without string parsing; Task 2 keeps the 32-pixel geometry, start-only header, optional duration, centered dot, visible title prefix, and structured full-title tooltip.
- Placeholder scan: no incomplete requirements or deferred implementation steps remain.
- Type consistency: Task 1 defines the values Task 2 consumes; Task 2 defines the layout API exercised by its tests and WPF composition.
