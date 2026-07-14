# Timeline Viewport Dimensions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep appointment blocks fully opaque in the timeline center by making block geometry and the fixed fade viewport use identical explicit dimensions.

**Architecture:** `TimelineGrid.ActualWidth` becomes the sole horizontal layout measurement. `UpdateLayoutMetrics` assigns this width and the calculated lane height to the masked `BlocksViewport` and its `BlocksCanvas`, preventing WPF's implicit child sizing from defining a separate mask coordinate system.

**Tech Stack:** .NET 10, C#, WPF, xUnit.

## Global Constraints

- Preserve the viewport opacity-mask stops `0`, `.12`, `.88`, and `1` and its `RelativeToBoundingBox` mapping.
- The opacity mask remains on `BlocksViewport`, never on `BlocksCanvas` or individual bubbles.
- Keep the vertical `CreateBubbleFill(colors.LightFill, colors.DarkFill)` gradient, bubble border, shadow, and interaction states unchanged.
- Keep unbounded block positions and widths plus the 52 DIP minimum block width unchanged.
- Now-line, current-time indicator, and countdown remain unmasked at their existing z-indices.
- Do not modify existing unrelated untracked files under `.superpowers/` or `docs/`.

---

### Task 1: Couple Viewport Dimensions to Timeline Geometry

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml:119-134`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs:335-382`
- Modify: `tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs:278-305`

**Interfaces:**
- Consumes: `TimelineGrid.ActualWidth` and `TimelineSnapbarLayout.GetTimelineHeight(int)`.
- Produces: a `BlocksViewport` and `BlocksCanvas` whose `Width` equals the geometry width and whose `Height` equals the lane-derived timeline height.

- [ ] **Step 1: Write the failing source regression test**

Add this fact to `Task7ReviewFixTests`:

```csharp
[Fact]
public void SnapbarSourceUsesTimelineGridDimensionsForBlockGeometryAndMasking()
{
    var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
    var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
    var viewportStart = xaml.IndexOf("<Grid x:Name=\"BlocksViewport\"", StringComparison.Ordinal);
    var canvasStart = xaml.IndexOf("<Canvas x:Name=\"BlocksCanvas\"", StringComparison.Ordinal);
    var viewport = xaml[viewportStart..xaml.IndexOf("</Grid>", canvasStart, StringComparison.Ordinal)];
    var updateLayout = source[
        source.IndexOf("private void UpdateLayoutMetrics", StringComparison.Ordinal)..
        source.IndexOf("private void UpdateWindowHeight", StringComparison.Ordinal)];

    Assert.Contains("var timelineWidth = TimelineGrid.ActualWidth;", updateLayout);
    Assert.Contains("BlocksViewport.Width = timelineWidth;", updateLayout);
    Assert.Contains("BlocksViewport.Height = timelineHeight;", updateLayout);
    Assert.Contains("BlocksCanvas.Width = timelineWidth;", updateLayout);
    Assert.Contains("BlocksCanvas.Height = timelineHeight;", updateLayout);
    Assert.Contains("HorizontalAlignment=\"Left\"", viewport);
    Assert.Contains("VerticalAlignment=\"Top\"", viewport);
}
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```bash
dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests"
```

Expected: FAIL because `UpdateLayoutMetrics` still calculates `timelineWidth` from `ActualWidth - TimelineWidthPadding` and assigns no dimensions to `BlocksViewport` or `BlocksCanvas`.

- [ ] **Step 3: Make the minimum production change**

Set the viewport's XAML alignment:

```xml
<Grid x:Name="BlocksViewport"
      HorizontalAlignment="Left"
      VerticalAlignment="Top"
      Panel.ZIndex="2"
      ClipToBounds="True">
```

In `UpdateLayoutMetrics`, replace the width calculation and assign all block-layer dimensions before adding buttons:

```csharp
var timelineWidth = TimelineGrid.ActualWidth;
BlocksViewport.Width = timelineWidth;
BlocksViewport.Height = timelineHeight;
BlocksCanvas.Width = timelineWidth;
BlocksCanvas.Height = timelineHeight;
TimelineGrid.Height = timelineHeight;
```

Remove `TimelineWidthPadding` if it has no remaining references.

- [ ] **Step 4: Run focused regressions and verify they pass**

Run:

```bash
dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests|FullyQualifiedName~TimelineSnapbarLayoutTests"
```

Expected: PASS with no failures.

- [ ] **Step 5: Build the WPF project**

Run:

```bash
dotnet build src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore
```

Expected: BUILD SUCCEEDED with no errors.

- [ ] **Step 6: Commit the completed task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs
git commit -m "fix(timeline): couple fade viewport to layout geometry" -m "Use the arranged TimelineGrid width as the shared coordinate system so WPF cannot size the opacity mask independently of appointment placement."
```

### Task 2: Validate the Completed Change

**Files:**
- Verify: `src/CalendarTimeline/CalendarTimeline.sln`
- Verify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj`

**Interfaces:**
- Consumes: the Task 1 shared-dimension invariant.
- Produces: test and build evidence that the fixed fade preserves existing timeline behavior.

- [ ] **Step 1: Run the complete test suite**

Run:

```bash
dotnet test src/CalendarTimeline/CalendarTimeline.sln --no-restore
```

Expected: PASS. If the known nested worker/host integration checks fail only because this Linux environment cannot reach NuGet's vulnerability endpoint, record that environment limitation separately and verify all remaining tests pass.

- [ ] **Step 2: Inspect the final diff and commits**

Run:

```bash
git diff --check HEAD~1..HEAD
git status --short
git log --oneline -3
```

Expected: no whitespace errors; only the intended task files are committed; pre-existing untracked artifacts remain unmodified.

- [ ] **Step 3: Commit the verification record only if a tracked verification artifact is added**

No verification-only source or documentation changes are required. Do not create an empty commit.
