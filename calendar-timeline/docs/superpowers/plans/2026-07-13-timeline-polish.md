# Timeline Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Polish the WPF timeline with fading bubbles, rounded interaction states, useful labels, time indicators, and height that shrinks only when auto-added lanes disappear.

**Architecture:** Keep rendering in `MainWindow` and isolate deterministic time calculations in the existing Snapbar project, so they can be unit-tested without WPF. Track a user-height floor in the window separately from lane-driven required height; only programmatic size updates may exceed that floor.

**Tech Stack:** .NET 10, C# 13, WPF, xUnit.

## Global Constraints

- Target `net10.0` and `net10.0-windows`; warnings remain errors.
- Preserve Outlook retrieval, existing calendar color rules, timeline time range, Teams link behavior, and manual resize support.
- Use local time for the now-line, `HH:mm` for clock and countdown, and `dddd, dd.MM.yyyy` for the German date tooltip.
- Only mask the appointment canvas; the now-line and its labels must remain unmasked.
- Keep the bubble minimum width at 52 DIP and retain full existing tooltip content.

---

### Task 1: Add deterministic time-display projection

**Files:**
- Create: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineTimeDisplay.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineBlockViewModel.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineTimeDisplayTests.cs`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`

**Interfaces:**
- Produces `TimelineTimeDisplay.GetCountdown(DateTimeOffset now, IEnumerable<TimelineBlockViewModel> blocks): string?`.
- `TimelineBlockViewModel` exposes its appointment start as `DateTimeOffset Start` for the WPF renderer and time-display helper.

- [ ] **Step 1: Write failing time-display tests**

```csharp
[Fact]
public void GetCountdown_ReturnsNearestFiveMinuteDurationForNextEvent()
{
    var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);
    var block = CreateBlock(start: now.AddMinutes(82), end: now.AddMinutes(112));

    Assert.Equal("01:20", TimelineTimeDisplay.GetCountdown(now, [block]));
}

[Fact]
public void GetCountdown_ReturnsNullWhileAnEventIsRunningOrNoFutureEventExists()
{
    var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

    Assert.Null(TimelineTimeDisplay.GetCountdown(now, [CreateBlock(now.AddMinutes(-10), now.AddMinutes(10))]));
    Assert.Null(TimelineTimeDisplay.GetCountdown(now, []));
}
```

- [ ] **Step 2: Run the new tests and verify compilation fails because `TimelineTimeDisplay` does not exist**

Run: `dotnet test tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter FullyQualifiedName~TimelineTimeDisplayTests`

Expected: failure mentioning `TimelineTimeDisplay`.

- [ ] **Step 3: Implement the projection and retain appointment start in the block projection**

```csharp
public static string? GetCountdown(DateTimeOffset now, IEnumerable<TimelineBlockViewModel> blocks)
{
    var materialized = blocks.ToArray();
    if (materialized.Any(block => block.Start <= now && now < block.End))
    {
        return null;
    }

    var next = materialized.Where(block => block.Start > now).OrderBy(block => block.Start).FirstOrDefault();
    if (next is null)
    {
        return null;
    }

    var roundedMinutes = Math.Round((next.Start - now).TotalMinutes / 5, MidpointRounding.AwayFromZero) * 5;
    return TimeSpan.FromMinutes(roundedMinutes).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
}
```

Pass the source appointment's start and end into `TimelineBlockViewModel` from `TimelineSnapbarViewModel`, and update its construction sites and property-shape test.

- [ ] **Step 4: Run the focused tests and verify they pass**

Run: `dotnet test tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineTimeDisplayTests|FullyQualifiedName~TimelineSnapbarViewModelTests"`

Expected: PASS.

### Task 2: Make automatic lane height reversible

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarLayout.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarLayoutTests.cs`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- `TimelineSnapbarLayout.GetWindowHeight(double manualHeight, double requiredHeight)` returns `Math.Max(manualHeight, requiredHeight)`.
- `MainWindow` stores `manualWindowHeight` and uses `isUpdatingWindowHeight` to distinguish a user resize from its own lane update.

- [ ] **Step 1: Replace the grow-only wording with failing tests for manual-floor semantics**

```csharp
[Theory]
[InlineData(48, 114, 114)]
[InlineData(120, 42, 120)]
public void GetWindowHeight_UsesManualFloorAndCurrentRequiredHeight(
    double manualHeight, double requiredHeight, double expectedHeight)
{
    Assert.Equal(expectedHeight, TimelineSnapbarLayout.GetWindowHeight(manualHeight, requiredHeight));
}
```

Add source assertions that `MainWindow` has both `manualWindowHeight` and `isUpdatingWindowHeight` and that only a non-programmatic `SizeChanged` updates the manual floor.

- [ ] **Step 2: Run layout tests to establish the source assertions fail**

Run: `dotnet test tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineSnapbarLayoutTests|FullyQualifiedName~Task7ReviewFixTests"`

Expected: failure because the window does not track manual height separately.

- [ ] **Step 3: Track manual height separately in `MainWindow`**

Initialize `manualWindowHeight` from the startup height. In `OnSizeChanged`, update that floor only when `isUpdatingWindowHeight` is false. In `UpdateWindowHeight`, compute `targetHeight` with `manualWindowHeight`, set `MinHeight` to the lane/status requirement, then set `Height` inside a `try/finally` guarded by `isUpdatingWindowHeight`. This makes a lane expansion reversible back to the manual floor.

- [ ] **Step 4: Run focused layout tests and verify they pass**

Run: `dotnet test tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineSnapbarLayoutTests|FullyQualifiedName~Task7ReviewFixTests"`

Expected: PASS.

### Task 3: Render the polished timeline controls

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- `TimelineTimeDisplay.GetCountdown(DateTimeOffset, IEnumerable<TimelineBlockViewModel>)` supplies the optional now-line countdown.
- `MainWindow.UpdateLayoutMetrics()` positions `NowTimeTextBlock`, `CountdownTextBlock`, and the existing now line from `TimelineSnapbarLayout.NowRatio`.

- [ ] **Step 1: Add failing structural tests for the visual contract**

```csharp
Assert.Contains("OpacityMask", blocksCanvas);
Assert.Contains("x:Name=\"NowTimeTextBlock\"", xaml);
Assert.Contains("x:Name=\"CountdownTextBlock\"", xaml);
Assert.Contains("ToolTip=\"{Binding ToolTip, RelativeSource={RelativeSource Self}}\"", nowTimeTextBlock);
Assert.Contains("ControlTemplate", source);
Assert.Contains("Text = block.StartTime + \" · \"", source);
```

Assert that the block content uses a horizontal layout with a trimming title, and that no default button border or background remains responsible for hover chrome.

- [ ] **Step 2: Run structural tests and verify they fail**

Run: `dotnet test tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter FullyQualifiedName~Task7ReviewFixTests`

Expected: FAIL because the fade mask, labels, template, and one-line label do not yet exist.

- [ ] **Step 3: Implement the XAML and code-behind rendering**

Add a left-to-right transparent/opaque/opaque/transparent opacity mask to `BlocksCanvas` matching the rail stops. Add a hit-testable now-line label container above the 2-DIP line; render local `HH:mm`, assign the long German date to its tooltip, and render a visible countdown only when `TimelineTimeDisplay.GetCountdown` returns a value.

In `CreateBlockButton`, replace default chrome with an inline `ControlTemplate` whose rounded border changes opacity on hover/pressed/focus states. Change `CreateBubbleContent` to a one-line horizontal `StackPanel` containing a nonshrinking `StartTime + " · "` `TextBlock` and a remaining-width title `TextBlock` with character ellipsis.

- [ ] **Step 4: Run all solution tests and build the WPF project**

Run: `dotnet test CalendarTimeline.sln`

Expected: PASS.

Run: `dotnet build src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore`

Expected: build succeeds with zero warnings and zero errors.

- [ ] **Step 5: Review the diff and commit the implementation**

Run: `git status --short`, `git diff --check`, `git diff`, and `git log --oneline -10`.

Stage only the implementation, tests, design, and plan files for this feature. Commit with:

```bash
git commit -m "feat(timeline): polish bubbles and time indicators"
```
