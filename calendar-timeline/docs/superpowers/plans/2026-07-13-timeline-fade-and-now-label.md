# Timeline Fade And Now Label Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep appointment fades fixed to the timeline while separating and correctly anchoring the current-time and countdown indicators.

**Architecture:** `BlocksCanvas` already owns a timeline-relative horizontal opacity mask, so it remains the single fixed fade layer for all projected appointment buttons. The WPF view splits the current combined time/countdown border into two independently positioned elements; `UpdateLayoutMetrics` supplies their margins from the existing now-line and timeline dimensions.

**Tech Stack:** .NET, WPF/XAML, C#, xUnit

## Global Constraints

- Do not change calendar window, block projection, Outlook retrieval, countdown calculation, or window-resize behavior.
- Keep the fade mask on `BlocksCanvas`, with transparent endpoints at 0 and 100 percent and full opacity from 12 through 88 percent.
- Keep the now line and both time indicators unmasked at a z-index above appointment blocks.
- Render local current time as `HH:mm`; retain the existing localized date tooltip and countdown visibility rules.
- Position current time left of the now line with its lower edge aligned to the line's lower edge; position countdown right of the now line and vertically centered in `TimelineGrid`.

---

### Task 1: Lock the Fixed-Fade and Separate-Indicator Contract

**Files:**
- Modify: `calendar-timeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs:200-214`

**Interfaces:**
- Consumes: WPF element names `BlocksCanvas`, `NowLine`, `NowTimeIndicator`, `NowTimeTextBlock`, and the new `CountdownIndicator` / existing `CountdownTextBlock`.
- Produces: source-level regression coverage for the fixed mask and separate positioning contract.

- [ ] **Step 1: Add a failing source-structure test**

Add this fact after `SnapbarSourceRendersPolishedBlocksAndNowIndicators`:

```csharp
[Fact]
public void SnapbarSourceKeepsTimelineFadesFixedAndSeparatesTimeIndicators()
{
    var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
    var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
    var blocksCanvas = xaml[xaml.IndexOf("<Canvas x:Name=\"BlocksCanvas\"", StringComparison.Ordinal)..];
    var nowTime = xaml[xaml.IndexOf("<Border x:Name=\"NowTimeIndicator\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"NowTimeIndicator\"", StringComparison.Ordinal), StringComparison.Ordinal)];
    var countdown = xaml[xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal), StringComparison.Ordinal)];

    Assert.Contains("<Canvas.OpacityMask>", blocksCanvas);
    Assert.Contains("Offset=\"0\"", blocksCanvas);
    Assert.Contains("Offset=\"0.12\"", blocksCanvas);
    Assert.Contains("Offset=\"0.88\"", blocksCanvas);
    Assert.Contains("Offset=\"1\"", blocksCanvas);
    Assert.Contains("HorizontalAlignment=\"Right\"", nowTime);
    Assert.Contains("VerticalAlignment=\"Bottom\"", nowTime);
    Assert.Contains("HorizontalAlignment=\"Left\"", countdown);
    Assert.Contains("VerticalAlignment=\"Center\"", countdown);
    Assert.Contains("timelineHeight - nowLineBounds.Bottom", source);
    Assert.Contains("timelineWidth - (timelineWidth * TimelineSnapbarLayout.NowRatio) + 4", source);
    Assert.Contains("CountdownIndicator.Margin", source);
}
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --no-restore --filter FullyQualifiedName~Task7ReviewFixTests.SnapbarSourceKeepsTimelineFadesFixedAndSeparatesTimeIndicators
```

Expected: FAIL because `CountdownIndicator` does not yet exist and the combined current indicator is top-aligned.

### Task 2: Split and Anchor the Two Time Indicators

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml:94-111`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs:331-341`
- Test: `calendar-timeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs:after SnapbarSourceRendersPolishedBlocksAndNowIndicators`

**Interfaces:**
- Consumes: `TimelineSnapbarLayout.NowRatio`, `TimelineSnapbarLayout.GetNowLineBounds()`, `timelineWidth`, `timelineHeight`, `NowTimeTextBlock`, and `CountdownTextBlock`.
- Produces: `NowTimeIndicator` as the current-time tooltip target, and `CountdownIndicator` as the independently placed countdown container.

- [ ] **Step 1: Replace the combined XAML indicator with two borders**

Replace the current `NowTimeIndicator` block with the following markup. Preserve `Panel.ZIndex="4"` on both elements so neither is affected by the canvas mask:

```xml
<Border x:Name="NowTimeIndicator"
        HorizontalAlignment="Right"
        VerticalAlignment="Bottom"
        Padding="4,1"
        Background="#B01B2635"
        CornerRadius="3"
        Panel.ZIndex="4">
  <TextBlock x:Name="NowTimeTextBlock"
             Foreground="White"
             FontSize="10"
             FontWeight="SemiBold" />
</Border>
<Border x:Name="CountdownIndicator"
        HorizontalAlignment="Left"
        VerticalAlignment="Center"
        Padding="4,1"
        Background="#B01B2635"
        CornerRadius="3"
        Panel.ZIndex="4">
  <TextBlock x:Name="CountdownTextBlock"
             Foreground="#DFFFFFFF"
             FontSize="10" />
</Border>
```

Leave the existing `BlocksCanvas.OpacityMask` unchanged. Its proportions are already fixed relative to the canvas, which makes blocks pass through fixed timeline fade zones as their projected positions refresh.

- [ ] **Step 2: Update the margin calculations in `UpdateLayoutMetrics`**

Replace the current `NowTimeIndicator.Margin` assignment and keep the existing text, tooltip, and visibility updates:

```csharp
var now = DateTimeOffset.Now;
NowTimeTextBlock.Text = now.ToString("HH:mm");
NowTimeIndicator.ToolTip = TimelineTimeDisplay.GetDateTooltip(now);
NowTimeIndicator.Margin = new Thickness(
    0,
    0,
    timelineWidth - (timelineWidth * TimelineSnapbarLayout.NowRatio) + 4,
    timelineHeight - nowLineBounds.Bottom);
CountdownIndicator.Margin = new Thickness(
    timelineWidth * TimelineSnapbarLayout.NowRatio + 4,
    0,
    0,
    0);
var countdown = TimelineTimeDisplay.GetCountdown(now, viewModel.Blocks);
CountdownTextBlock.Text = countdown ?? string.Empty;
CountdownIndicator.Visibility = countdown is null ? Visibility.Collapsed : Visibility.Visible;
```

The right margin makes the current-time border end four DIP left of the now line. The bottom margin aligns that border's lower edge to `nowLineBounds.Bottom`, even when additional lanes increase `timelineHeight`. `VerticalAlignment="Center"` keeps the countdown centered within the entire timeline grid while its left margin keeps it four DIP right of the line.

- [ ] **Step 3: Run the focused test to verify it passes**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --no-restore --filter FullyQualifiedName~Task7ReviewFixTests.SnapbarSourceKeepsTimelineFadesFixedAndSeparatesTimeIndicators
```

Expected: PASS.

- [ ] **Step 4: Run all timeline-related unit tests**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~TimelineSnapbarLayoutTests|FullyQualifiedName~TimelineTimeDisplayTests|FullyQualifiedName~Task7ReviewFixTests"
```

Expected: PASS with no regressions in fade-mask invariants, countdown formatting, or layout behavior.

- [ ] **Step 5: Commit the implementation**

```bash
git add calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs calendar-timeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs
git commit -m "fix(timeline): anchor fades and time indicators"
```
