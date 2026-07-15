# Countdown bei parallelen Terminen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Den Countdown fuer den naechsten bevorstehenden Termin auch bei laufenden Terminen anzeigen, aktive Terminbloecke rechtzeitig hervorheben und den Countdown horizontal kollisionssicher positionieren.

**Architecture:** Die Snapbar-Bibliothek erhaelt reine Zeit- und Geometrieentscheidungen: Countdown-Ziel, Sichtbarkeit, Termin-Hervorhebung und eine erlaubte X-Position. WPF bleibt fuer Messung, konkrete Darstellung und Animation verantwortlich. Sie projiziert den Countdown-Zustand in die bestehende Anzeige und rendert die visuelle Hervorhebung fuer jeden Block ohne Text.

**Tech Stack:** .NET 10, C#, WPF, xUnit v3.

## Global Constraints

- Ein Countdown zaehlt immer auf den fruehesten gueltigen sichtbaren Termin mit `Start > jetzt`.
- Countdown-Sichtbarkeit endet ausschliesslich bei `<= 5 Minuten` Restzeit, fehlendem Zieltermin oder einem Ziel ausserhalb des sichtbaren Zeitfensters.
- Jeder gueltige sichtbare Termin wird von `Start - 5 Minuten` bis exklusiv `Ende` ohne Text hervorgehoben; parallele Hervorhebungen sind erlaubt.
- Der Countdown bewegt sich nur horizontal, darf laufende Termine teilweise ueberdecken und darf den angezaehlten Zieltermin nie ueberdecken.
- Die 3-px-Pendelbewegung pausiert waehrend einer rund 150 ms langen horizontalen Basispositionsanimation und setzt danach fort.
- Bestehende All-Day-Tag-, Fenster-, Block-, Farb- und Teams-Link-Verhalten bleiben unveraendert.
- Jede produktive Aenderung wird erst durch einen fehlschlagenden Test spezifiziert; jede abgeschlossene Aufgabe wird mit einer Conventional-Commit-Nachricht eingecheckt.

---

## File Structure

- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineTimeDisplay.cs`: waehlt das Countdown-Ziel, formatiert dessen Restzeit und bestimmt die unabhhaengige Termin-Hervorhebung.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineCountdown.cs`: definiert den unveraenderlichen Countdown-Zustand aus Text und Zielblock.
- `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineCountdownLayout.cs`: berechnet eine reine horizontale Countdown-Basisposition vor dem Zielblock.
- `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`: trennt Basispositions- und Pendel-Transform sowie benennt das Pendel-Storyboard fuer Steuerung aus Code-behind.
- `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`: misst die Anzeige, bestimmt Blockgrenzen, setzt Hervorhebung und animiert die Basisposition.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineTimeDisplayTests.cs`: deckt Zielauswahl, Fuenf-Minuten-Grenze, Wiedereinblendung und parallele Hervorhebungen ab.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineCountdownLayoutTests.cs`: deckt die reine horizontale Kollisions- und Zielgrenzenlogik ab.
- `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`: prueft die WPF-Quellstruktur, die unter Linux nicht ausgefuehrt werden kann.

### Task 1: Countdown-Ziel und Termin-Hervorhebung in Snapbar bestimmen

**Files:**
- Create: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineCountdown.cs`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineTimeDisplay.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineTimeDisplayTests.cs`

**Interfaces:**
- Produces `public sealed record TimelineCountdown(string Text, TimelineBlockViewModel Target)`.
- Produces `TimelineTimeDisplay.GetCountdown(DateTimeOffset now, IEnumerable<TimelineBlockViewModel> blocks): TimelineCountdown?`.
- Produces `TimelineTimeDisplay.IsHighlighted(DateTimeOffset now, TimelineBlockViewModel block): bool`.
- `GetCountdown` ignores invalid blocks, selects only `Start > now`, returns `null` at or below five minutes, and rounds text to five-minute intervals.
- `IsHighlighted` returns `true` when `block.Start - TimeSpan.FromMinutes(5) <= now && now < block.End` for valid blocks.

- [ ] **Step 1: Write failing countdown state tests**

```csharp
var running = CreateBlock(now.AddMinutes(-20), now.AddMinutes(30));
var next = CreateBlock(now.AddMinutes(82), now.AddMinutes(112));

var countdown = TimelineTimeDisplay.GetCountdown(now, [running, next]);

Assert.Equal("01:20", countdown!.Text);
Assert.Same(next, countdown.Target);
```

Add boundary assertions that `GetCountdown(now, [CreateBlock(now.AddMinutes(5), now.AddMinutes(35))])` is `null`, that a future event after a just-started target is selected, and that invalid blocks are ignored.

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineTimeDisplayTests"`

Expected: FAIL because `GetCountdown` still returns `string` and no target state exists.

- [ ] **Step 3: Add the immutable countdown state and minimal selection logic**

```csharp
public sealed record TimelineCountdown(string Text, TimelineBlockViewModel Target);

public static TimelineCountdown? GetCountdown(DateTimeOffset now, IEnumerable<TimelineBlockViewModel> blocks)
{
    var target = blocks
        .Where(block => block.End > block.Start && block.Start > now)
        .OrderBy(block => block.Start)
        .FirstOrDefault();
    if (target is null || target.Start - now <= TimeSpan.FromMinutes(5))
    {
        return null;
    }

    var minutes = (int)(Math.Round((target.Start - now).TotalMinutes / 5, MidpointRounding.AwayFromZero) * 5);
    return new TimelineCountdown($"{minutes / 60:D2}:{minutes % 60:D2}", target);
}
```

- [ ] **Step 4: Write failing highlight tests**

```csharp
var block = CreateBlock(now.AddMinutes(5), now.AddMinutes(35));

Assert.True(TimelineTimeDisplay.IsHighlighted(now, block));
Assert.True(TimelineTimeDisplay.IsHighlighted(now.AddMinutes(5), block));
Assert.False(TimelineTimeDisplay.IsHighlighted(now.AddMinutes(35), block));
Assert.True(TimelineTimeDisplay.IsHighlighted(now, CreateBlock(now.AddMinutes(-2), now.AddMinutes(20))));
```

- [ ] **Step 5: Run the focused test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineTimeDisplayTests"`

Expected: FAIL because `IsHighlighted` does not exist.

- [ ] **Step 6: Implement the per-block highlight predicate**

```csharp
public static bool IsHighlighted(DateTimeOffset now, TimelineBlockViewModel block)
{
    return block.End > block.Start
        && block.Start - TimeSpan.FromMinutes(5) <= now
        && now < block.End;
}
```

- [ ] **Step 7: Run focused and full tests**

Run: `dotnet test CalendarTimeline.sln --no-restore`

Expected: PASS with all tests green.

- [ ] **Step 8: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineCountdown.cs src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineTimeDisplay.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineTimeDisplayTests.cs
git commit -m "feat(timeline): select countdown targets during active events" -m "Make the next start independent of concurrent meetings while using one five-minute boundary for the calm countdown handoff and per-event visual readiness."
```

### Task 2: Berechenbare horizontale Countdown-Position einfuehren

**Files:**
- Create: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineCountdownLayout.cs`
- Create: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineCountdownLayoutTests.cs`

**Interfaces:**
- Produces `public readonly record struct TimelineHorizontalBounds(double Left, double Width)` with `Right`.
- Produces `TimelineCountdownLayout.GetLeft(double baseLeft, double indicatorWidth, double targetLeft, IEnumerable<TimelineHorizontalBounds> runningBlocks): double`.
- `GetLeft` uses the greatest right edge of running blocks extending past `baseLeft`, then caps the result at `targetLeft - indicatorWidth` so the target is never covered.

- [ ] **Step 1: Write failing layout tests**

```csharp
Assert.Equal(32, TimelineCountdownLayout.GetLeft(32, 20, 160, []));
Assert.Equal(108, TimelineCountdownLayout.GetLeft(32, 20, 160, [new TimelineHorizontalBounds(20, 88)]));
Assert.Equal(140, TimelineCountdownLayout.GetLeft(32, 20, 160, [new TimelineHorizontalBounds(20, 150)]));
```

The second assertion proves horizontal movement behind a running block. The third proves the target cap (`160 - 20`) wins even if the running block overlaps the target in time.

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~TimelineCountdownLayoutTests"`

Expected: FAIL because `TimelineCountdownLayout` does not exist.

- [ ] **Step 3: Implement the pure geometry rule**

```csharp
public static double GetLeft(
    double baseLeft,
    double indicatorWidth,
    double targetLeft,
    IEnumerable<TimelineHorizontalBounds> runningBlocks)
{
    var afterRunningBlocks = runningBlocks
        .Where(block => block.Right > baseLeft)
        .Select(block => block.Right)
        .DefaultIfEmpty(baseLeft)
        .Max();
    return Math.Min(afterRunningBlocks, targetLeft - indicatorWidth);
}
```

- [ ] **Step 4: Run focused and full tests**

Run: `dotnet test CalendarTimeline.sln --no-restore`

Expected: PASS with all tests green.

- [ ] **Step 5: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineCountdownLayout.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineCountdownLayoutTests.cs
git commit -m "feat(timeline): constrain countdown ahead of its target" -m "Keep the indicator on a stable vertical rail, allow it to pass active blocks when space is constrained, and make the counted appointment an inviolable horizontal boundary."
```

### Task 3: Countdown und Hervorhebung in WPF rendern und animieren

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Modify: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- Consumes `TimelineTimeDisplay.GetCountdown`, `TimelineTimeDisplay.IsHighlighted`, `TimelineCountdown.Target`, `TimelineCountdownLayout.GetLeft`, and `TimelineHorizontalBounds`.
- WPF measures `CountdownIndicator.DesiredSize.Width` before calling `GetLeft`.
- WPF renders each highlighted block with a strengthened border plus a translucent overlay, with a repeat animation only while it remains highlighted.
- WPF owns separate base and pendulum `TranslateTransform` instances so the base transition can pause and restart the pendulum storyboard.

- [ ] **Step 1: Write failing WPF source-structure tests**

Add assertions that the XAML contains a named base transform and named pendulum transform, names the pendulum storyboard, and retains the 3-pixel `AutoReverse` repeat. Add source assertions for `TimelineTimeDisplay.IsHighlighted(now, block)`, `TimelineCountdownLayout.GetLeft`, `CountdownIndicator.Measure`, a `DoubleAnimation` whose duration is 150 ms, and start/stop calls for the pendulum storyboard. Assert the bubble factory receives the highlight flag and creates an overlay border only for highlighted blocks.

- [ ] **Step 2: Run the source-structure test to verify it fails**

Run: `dotnet test CalendarTimeline.sln --no-restore --filter "FullyQualifiedName~Task7ReviewFixTests"`

Expected: FAIL because the countdown has one transform, no measured collision geometry, and bubbles only use the old `IsRunning` shadow.

- [ ] **Step 3: Split transforms and name the pendulum storyboard in XAML**

Replace the sole `CountdownTranslation` transform with a `TransformGroup` containing `CountdownBaseTranslation` and `CountdownWobbleTranslation`. Point the existing 0-to-3, 1.2-second, auto-reversing infinite animation at `CountdownWobbleTranslation.X` and give its `BeginStoryboard` a name that code-behind can stop and begin after repositioning.

- [ ] **Step 4: Measure, place, and animate the countdown in code-behind**

In `UpdateLayoutMetrics`, obtain the `TimelineCountdown?`, assign its text before measurement, call `CountdownIndicator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity))`, derive every running block's `TimelineHorizontalBounds` through the existing `GetBlockBounds`, and calculate the target block's left edge using the same method. Use `TimelineCountdownLayout.GetLeft` for the base X coordinate. If this differs from the current base transform, stop the wobble storyboard, animate `CountdownBaseTranslation.X` over 150 ms with `QuadraticEase { EasingMode = EasingMode.EaseOut }`, and restart wobble from the animation completion handler. Keep the countdown collapsed only when the state is `null`.

- [ ] **Step 5: Render independent visual block highlighting**

Pass `TimelineTimeDisplay.IsHighlighted(now, block)` from the block loop into `CreateBlockButton` and then `CreateBubbleContent`. For highlighted blocks, retain the resolved fill, increase the border thickness, add a semi-transparent white overlay border above the existing label, and animate only that overlay's opacity with an auto-reversing repeat. Do not add text or alter click handling, title layout, tooltip, lane assignment, color selection, or the existing running shadow.

- [ ] **Step 6: Run all tests and build the Windows WPF project**

Run: `dotnet test CalendarTimeline.sln --no-restore && dotnet build src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj --no-restore`

Expected: PASS with no warnings or errors.

- [ ] **Step 7: Commit the task**

```bash
git add src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs
git commit -m "feat(timeline): animate countdown around active event blocks" -m "Preserve the familiar resting pulse while making position changes legible, and signal every imminent or active appointment through non-textual emphasis."
```

## Plan Review

- Spec coverage: Task 1 implements selection, the five-minute visibility handoff, immediate reselection, and simultaneous per-block highlighting. Task 2 guarantees horizontal-only placement against running blocks and the target boundary. Task 3 supplies rendering, measured geometry, transition behavior, retained pendulum animation, and WPF regression checks.
- Placeholder scan: no incomplete requirements or deferred implementation steps remain.
- Type consistency: Task 1 introduces the state consumed by Task 3. Task 2 owns the bounds and placement API consumed by Task 3. The public names and return types are identical in all task descriptions.
