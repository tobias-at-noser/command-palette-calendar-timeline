# Timeline Absolute Fade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Anchor appointment fading to exact temporal positions across the full timeline instead of to rendered block bounds.

**Architecture:** `TimelineSnapbarLayout` exposes the exact shared fade ratios for the configured calendar window. The rail consumes those ratios directly, while the appointment viewport uses an absolute-coordinate opacity brush whose endpoint is updated from `TimelineGrid.ActualWidth` during every layout pass.

**Tech Stack:** .NET 10, C#, WPF, XAML, xUnit

## Global Constraints

- The timeline window remains `now - 30 minutes` through `now + 4 hours`.
- Fade stops are exactly `0`, `1 / 9`, `8 / 9`, and `1`.
- The left fade reaches full opacity at the Now line; the right fade begins at `now + 3.5 hours`.
- The rail and block mask consume the same shared layout constants.
- The block mask uses absolute timeline coordinates and is recalculated from the current timeline width on layout.
- Unbounded appointment projection, clipping, 52 DIP minimum block width, lane geometry, styling, interactions, time indicators, and window sizing remain unchanged.
- Now line, current-time indicator, and countdown indicator remain unmasked at their existing Z-indices.
- Automated tests are written and observed failing before each production change.
- Existing unrelated worktree changes and untracked files are not modified or committed.
- Final visual verification is performed on Windows because the Linux test environment cannot execute WPF rendering.

---

### Task 1: Shared Temporal Fade Ratios

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarLayout.cs:5-11`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarLayoutTests.cs:89-94`

**Interfaces:**
- Consumes: `TimelineSnapbarLayout.NowRatio` with value `1d / 9d`
- Produces: `public const double FadeInEndRatio` and `public const double FadeOutStartRatio`

- [ ] **Step 1: Write the failing ratio test**

Replace `NowRatio_MatchesTheConfiguredCalendarWindow` with:

```csharp
[Fact]
public void FadeRatios_MatchTheConfiguredCalendarWindow()
{
    Assert.Equal(1d / 9d, TimelineSnapbarLayout.NowRatio, 10);
    Assert.Equal(TimelineSnapbarLayout.NowRatio, TimelineSnapbarLayout.FadeInEndRatio, 10);
    Assert.Equal(8d / 9d, TimelineSnapbarLayout.FadeOutStartRatio, 10);
    Assert.Equal(
        1d,
        TimelineSnapbarLayout.FadeInEndRatio + TimelineSnapbarLayout.FadeOutStartRatio,
        10);
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineSnapbarLayoutTests"
```

Expected: compilation fails because `FadeInEndRatio` and `FadeOutStartRatio` do not exist.

- [ ] **Step 3: Add the shared constants**

Immediately after `NowRatio`, add:

```csharp
public const double FadeInEndRatio = NowRatio;
public const double FadeOutStartRatio = 1d - NowRatio;
```

- [ ] **Step 4: Run the focused test and verify GREEN**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineSnapbarLayoutTests"
```

Expected: PASS with no failed tests.

- [ ] **Step 5: Commit the tested contract**

Stage only the two files from this task and commit with a Conventional Commit message. The body must explain that the constants encode temporal boundaries and prevent rail/mask drift rather than merely restating that constants were added.

---

### Task 2: Absolute Timeline Mask

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml:68-82,119-136`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs:334-343`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs:252-326`

**Interfaces:**
- Consumes: `TimelineSnapbarLayout.FadeInEndRatio`, `TimelineSnapbarLayout.FadeOutStartRatio`, and `timelineWidth`
- Produces: named `LinearGradientBrush` field `TimelineFadeMask` with absolute `(0, 0)` to `(timelineWidth, 0)` coordinates

- [ ] **Step 1: Update source-contract tests for the required mask geometry**

In `SnapbarSourceKeepsTimelineFadesFixedAndSeparatesTimeIndicators`, replace the `.12` and `.88` assertions with assertions for both shared static references:

```csharp
Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeInEndRatio}\"", blocksViewport);
Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeOutStartRatio}\"", blocksViewport);
```

In `SnapbarSourceUsesAClippedMaskedViewportForUnboundedBlocks`, require:

```csharp
Assert.Contains("x:Name=\"TimelineFadeMask\"", viewport);
Assert.Contains("MappingMode=\"Absolute\"", viewport);
Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeInEndRatio}\"", viewport);
Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeOutStartRatio}\"", viewport);
Assert.DoesNotContain("MappingMode=\"RelativeToBoundingBox\"", viewport);
```

Remove the obsolete expectations for `EndPoint="1,0"`, `.12`, and `.88`.

Rename `SnapbarSourceUsesTimelineGridDimensionsForBlockGeometryAndMasking` to `SnapbarSourceUsesTimelineGridDimensionsForBlockGeometryAndAbsoluteMasking`, read `MainWindow.xaml.cs`, and add:

```csharp
Assert.Contains("TimelineFadeMask.StartPoint = new Point(0, 0);", updateLayout);
Assert.Contains("TimelineFadeMask.EndPoint = new Point(timelineWidth, 0);", updateLayout);
```

Also assert that the full XAML contains each shared static reference twice, once for the rail and once for the viewport mask:

```csharp
Assert.Equal(2, CountOccurrences(xaml, "TimelineSnapbarLayout.FadeInEndRatio"));
Assert.Equal(2, CountOccurrences(xaml, "TimelineSnapbarLayout.FadeOutStartRatio"));
```

Add this helper near the existing test helpers:

```csharp
private static int CountOccurrences(string source, string value)
{
    var count = 0;
    var startIndex = 0;

    while ((startIndex = source.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
    {
        count++;
        startIndex += value.Length;
    }

    return count;
}
```

- [ ] **Step 2: Run the structural tests and verify RED**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests"
```

Expected: FAIL because the XAML still uses relative bounding-box coordinates and decimal approximations, and the code-behind does not set absolute brush points.

- [ ] **Step 3: Bind the rail and mask to the shared ratios**

In both horizontal gradients, replace the `.12` and `.88` stops with:

```xml
<GradientStop Color="#CC6A8296"
              Offset="{x:Static snapbar:TimelineSnapbarLayout.FadeInEndRatio}" />
<GradientStop Color="#CC6A8296"
              Offset="{x:Static snapbar:TimelineSnapbarLayout.FadeOutStartRatio}" />
```

Use `Color="White"` rather than `#CC6A8296` for the two viewport-mask stops. Keep the existing transparent edge colors and all existing rail styling.

- [ ] **Step 4: Convert the viewport mask to absolute coordinates**

Change the viewport brush opening to:

```xml
<LinearGradientBrush x:Name="TimelineFadeMask"
                     StartPoint="0,0"
                     EndPoint="0,0"
                     MappingMode="Absolute">
```

The zero endpoint is only the XAML initialization value; layout supplies the current timeline width before blocks are rendered.

- [ ] **Step 5: Couple the absolute brush endpoint to layout**

In `UpdateLayoutMetrics()`, immediately after `var timelineWidth = TimelineGrid.ActualWidth;`, add:

```csharp
TimelineFadeMask.StartPoint = new Point(0, 0);
TimelineFadeMask.EndPoint = new Point(timelineWidth, 0);
```

Do not alter viewport/canvas dimensions, block placement, clipping, Z-order, or any vertical geometry.

- [ ] **Step 6: Run focused tests and verify GREEN**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineSnapbarLayoutTests|FullyQualifiedName~Task7ReviewFixTests"
```

Expected: PASS with no failed tests.

- [ ] **Step 7: Build WPF and commit the root-cause fix**

Run:

```bash
dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore
```

Expected: build succeeds with 0 warnings and 0 errors.

Stage only the three files from this task and commit with a Conventional Commit message. The body must explain why absolute brush space is necessary despite explicit viewport dimensions and why the endpoint is refreshed during layout.

---

### Task 3: Full Verification

**Files:**
- Verify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj`
- Verify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj`

**Interfaces:**
- Consumes: all changes from Tasks 1 and 2
- Produces: automated regression evidence and a Windows visual-acceptance checklist

- [ ] **Step 1: Run the complete test project**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj
```

Expected: PASS with no failed tests.

- [ ] **Step 2: Build the WPF project without restoring**

Run:

```bash
dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore
```

Expected: build succeeds with 0 warnings and 0 errors.

- [ ] **Step 3: Inspect the task diff**

Confirm that production changes are limited to the shared ratios, the two gradient stops, the viewport mask coordinate system, and layout-time mask points. Confirm that unrelated untracked files remain unmodified and uncommitted.

- [ ] **Step 4: Perform Windows visual acceptance**

Run the WPF application on Windows and verify:

1. A centered short appointment is fully opaque across its complete width.
2. At the left edge, opacity is zero at `now - 30 minutes` and reaches full opacity exactly at the Now line.
3. At the right edge, opacity remains full through `now + 3.5 hours` and reaches zero at `now + 4 hours`.
4. Resizing keeps both fade regions fixed to those temporal positions.
5. Removing or moving appointments does not move or resize either fade region.
6. Now line, current time, and countdown remain unmasked.

If Windows visual execution is unavailable in the current environment, report this item explicitly as pending rather than claiming it passed.
