# Buendige obere Kante der Timeline-Snapbar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the visible Timeline-Snapbar flush with the top of the display while retaining left, right, and bottom resize interactions.

**Architecture:** Keep native non-client hit testing, but remove all top-edge resize directions from the UI-independent interaction helper. Remove only the top visual margin in WPF and reduce the height accounting by the same amount; geometry persistence, dragging, and appointment clicks remain unchanged.

**Tech Stack:** .NET 10, C#, WPF (`net10.0-windows`), xUnit v3, Win32 `WM_NCHITTEST` interop.

## Global Constraints

- A Snapbar at `Top = 0` must visibly touch the top screen edge.
- The top edge and top corners must not expose vertical or diagonal resizing.
- Keep left, right, bottom, bottom-left, and bottom-right native resizing.
- Preserve free-space dragging and appointment button click behavior.
- Do not alter persisted geometry contracts or create commits unless the user explicitly requests one.

---

### Task 1: Remove Top Resize Zones and Top Visual Margin

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/SnapbarWindowInteraction.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/SnapbarWindowInteractionTests.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`

**Interfaces:**
- Produces `SnapbarWindowInteraction.GetResizeDirection(double x, double y, double width, double height, double resizeBorder)` returning only `Left`, `Right`, `BottomLeft`, `Bottom`, `BottomRight`, or `None` for a top-edge pointer.
- Consumes the existing WPF `WndProc` mapping; it already treats `None` as a client hit-test and therefore needs no new Win32 behavior.

- [ ] **Step 1: Update the resize-direction unit test to describe top-edge client behavior**

```csharp
[Theory]
[InlineData(0, 0, SnapbarResizeDirection.Left)]
[InlineData(100, 0, SnapbarResizeDirection.None)]
[InlineData(199, 0, SnapbarResizeDirection.Right)]
[InlineData(199, 50, SnapbarResizeDirection.Right)]
[InlineData(199, 99, SnapbarResizeDirection.BottomRight)]
[InlineData(100, 99, SnapbarResizeDirection.Bottom)]
[InlineData(0, 99, SnapbarResizeDirection.BottomLeft)]
[InlineData(0, 50, SnapbarResizeDirection.Left)]
[InlineData(100, 50, SnapbarResizeDirection.None)]
public void GetResizeDirectionLeavesTheTopEdgeOutOfTheResizeFrame(
    double x,
    double y,
    SnapbarResizeDirection expected)
{
    Assert.Equal(expected, SnapbarWindowInteraction.GetResizeDirection(x, y, 200, 100, 8));
}
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter FullyQualifiedName~SnapbarWindowInteractionTests`

Expected: FAIL because `GetResizeDirection` currently returns `Top`, `TopLeft`, and `TopRight`.

- [ ] **Step 3: Make top coordinates ignore vertical resize while preserving side and bottom priority**

```csharp
var left = x <= resizeBorder;
var right = x >= width - resizeBorder;
var bottom = y >= height - resizeBorder;

if (bottom && right) return SnapbarResizeDirection.BottomRight;
if (bottom && left) return SnapbarResizeDirection.BottomLeft;
if (left) return SnapbarResizeDirection.Left;
if (right) return SnapbarResizeDirection.Right;
if (bottom) return SnapbarResizeDirection.Bottom;

return SnapbarResizeDirection.None;
```

- [ ] **Step 4: Remove the top visual inset and match height accounting**

Use `Margin="12,0,12,6"` for both `HoverSurface` and the content grid in `MainWindow.xaml`. In `MainWindow.xaml.cs`, change `GridVerticalMargin` from `12` to `6`, preserving the bottom padding in required-height calculation.

- [ ] **Step 5: Run focused tests and WPF build**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter FullyQualifiedName~SnapbarWindowInteractionTests`

Expected: PASS, with top-center returning `None`, top-left returning `Left`, and top-right returning `Right`.

Run: `dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj`

Expected: PASS with zero errors and zero warnings.

### Task 2: Verify the Integrated Regression Surface

**Files:**
- No source changes expected.

**Interfaces:**
- Consumes the completed interaction helper and WPF layout changes from Task 1.

- [ ] **Step 1: Run the full automated suite**

Run: `dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`

Expected: PASS with zero failed tests.

- [ ] **Step 2: Inspect the final diff**

Run: `git diff --check` and `git diff -- calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Snapbar/SnapbarWindowInteraction.cs calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/SnapbarWindowInteractionTests.cs calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`

Expected: no whitespace errors and only the planned top-edge interaction and visual-margin changes.

### Plan Self-Review

- [x] Spec coverage: Task 1 makes the visual surface flush, removes top resize behavior, preserves the remaining edges, and keeps existing drag and appointment handling untouched. Task 2 verifies integration.
- [x] Placeholder scan: no deferred implementation or undefined work remains.
- [x] Type consistency: the plan retains the existing `GetResizeDirection` signature and `SnapbarResizeDirection` values used by WPF.
