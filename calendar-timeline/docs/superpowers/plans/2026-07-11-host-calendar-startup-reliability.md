# Host Calendar Startup Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Start the Windows Host promptly, use the Windows-capable Outlook worker, and bound blocked Outlook refreshes.

**Architecture:** The Host project derives the worker framework from its current framework so Windows builds compile the Outlook COM branch. The Host opens its tray and Named Pipe server before scheduling the initial refresh. The worker source owns a bounded child-process lifetime and returns a safe timeout status through the existing Host cache and WPF view model.

**Tech Stack:** C#/.NET 10, MSBuild, xUnit, WPF, Windows Forms, System.Diagnostics.Process.

## Global Constraints

- Keep non-Windows builds on `net10.0` for Linux-based tests.
- Use `net10.0-windows10.0.19041.0` for the Windows worker that accesses Outlook COM.
- Do not expose arbitrary Outlook or process error text in the WPF status.
- Do not add external dependencies.

---

### Task 1: Select the Windows worker target

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Host/CalendarTimeline.Host.csproj:13-16`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/OutlookCalendarSnapshotSourceTests.cs`

**Interfaces:**
- Consumes: Host `$(TargetFramework)`.
- Produces: `$(WorkerTargetFramework)` with `net10.0-windows10.0.19041.0` for a Windows Host and `net10.0` otherwise.

- [ ] **Step 1: Write the failing regression test**

```csharp
[Fact]
public void HostProjectUsesWindowsWorkerTargetForWindowsHost()
{
    var project = XDocument.Load(HostFile("CalendarTimeline.Host.csproj"));
    var targetFrameworks = project.Descendants("WorkerTargetFramework").ToArray();

    Assert.Contains(targetFrameworks, element =>
        element.Attribute("Condition")?.Value.Contains("-windows", StringComparison.Ordinal) == true
        && element.Value == "net10.0-windows10.0.19041.0");
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run: `dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests --no-restore --filter "FullyQualifiedName~OutlookCalendarSnapshotSourceTests"`

Expected: FAIL because the Host project contains only a fixed `net10.0` worker target.

- [ ] **Step 3: Derive `WorkerTargetFramework` from `TargetFramework`**

```xml
<WorkerTargetFramework Condition="$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))">net10.0-windows10.0.19041.0</WorkerTargetFramework>
<WorkerTargetFramework Condition="!$([System.String]::Copy('$(TargetFramework)').Contains('-windows'))">net10.0</WorkerTargetFramework>
```

- [ ] **Step 4: Run the focused test**

Run: `dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests --no-restore --filter "FullyQualifiedName~OutlookCalendarSnapshotSourceTests"`

Expected: PASS.

### Task 2: Bound and report worker execution

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Host/WorkerHostSnapshotSource.cs:6-73`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Host/HostSnapshotCache.cs:20-30`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Host/CalendarTimelineHostService.cs:41-45`
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Snapbar/TimelineSnapbarViewModel.cs:102-106`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/WorkerHostSnapshotSourceTests.cs`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimelineHostServiceTests.cs`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarViewModelTests.cs`

**Interfaces:**
- Consumes: `WorkerHostSnapshotSource(string workerExecutablePath, TimeSpan timeout)`.
- Produces: a `TimeoutException` after the configured timeout and a safe Host error response of `Kalenderdaten nicht verfügbar: Outlook-Aktualisierung hat das Zeitlimit überschritten.`

- [ ] **Step 1: Write failing timeout and safe-status tests**

```csharp
var source = new WorkerHostSnapshotSource(workerPath, TimeSpan.FromMilliseconds(50));
await Assert.ThrowsAsync<TimeoutException>(() => source.LoadSnapshotAsync(TestContext.Current.CancellationToken));

var response = await service.HandleAsync(new RefreshSnapshotRequest(), TestContext.Current.CancellationToken);
Assert.Equal("Kalenderdaten nicht verfügbar: Outlook-Aktualisierung hat das Zeitlimit überschritten.", Assert.IsType<ErrorResponse>(response).Message);
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run: `dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests --no-restore --filter "FullyQualifiedName~WorkerHostSnapshotSourceTests|FullyQualifiedName~CalendarTimelineHostServiceTests|FullyQualifiedName~TimelineSnapbarViewModelTests"`

Expected: FAIL because the worker constructor has no timeout and the Host returns only the generic unavailable status.

- [ ] **Step 3: Add timeout-aware worker execution**

```csharp
using var timeoutSource = new CancellationTokenSource(timeout);
using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
// Use linkedSource.Token for stdout, stderr, and WaitForExitAsync.
// Translate timeout cancellation to TimeoutException after stopping the process tree.
```

- [ ] **Step 4: Map only timeout failures to a user-safe status**

```csharp
catch (Exception exception)
{
    cache.MarkUnavailable(exception is TimeoutException
        ? "Kalenderdaten nicht verfügbar: Outlook-Aktualisierung hat das Zeitlimit überschritten."
        : HostSnapshotCache.UnavailableStatus);
    return cache.GetSnapshotResponse();
}
```

- [ ] **Step 5: Preserve the response message in the snapbar**

```csharp
catch (Exception exception)
{
    Blocks.Clear();
    StatusText = string.IsNullOrWhiteSpace(exception.Message) ? UnavailableStatusText : exception.Message;
}
```

- [ ] **Step 6: Run the focused tests**

Run: `dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests --no-restore --filter "FullyQualifiedName~WorkerHostSnapshotSourceTests|FullyQualifiedName~CalendarTimelineHostServiceTests|FullyQualifiedName~TimelineSnapbarViewModelTests"`

Expected: PASS.

### Task 3: Start the Host before the first refresh

**Files:**
- Modify: `src/CalendarTimeline/src/CalendarTimeline.Host/Program.cs:19-72`
- Test: `src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- Consumes: `CalendarTimelineHostService.HandleAsync(RefreshSnapshotRequest, CancellationToken)`.
- Produces: a Windows Host that starts `CalendarTimelinePipeServer.RunAsync` and `Application.Run` without awaiting the initial refresh.

- [ ] **Step 1: Write a failing startup-order regression test**

```csharp
Assert.True(IndexOf(programSource, "var serverTask = server.RunAsync")
    < IndexOf(programSource, "_ = RefreshInitialSnapshotAsync"));
Assert.True(IndexOf(programSource, "_ = RefreshInitialSnapshotAsync")
    < IndexOf(programSource, "System.Windows.Forms.Application.Run(context);"));
```

- [ ] **Step 2: Run the test and verify it fails**

Run: `dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests --no-restore --filter "FullyQualifiedName~Task7ReviewFixTests"`

Expected: FAIL because `Program.Main` awaits the first refresh before calling `RunWindowsHostAsync`.

- [ ] **Step 3: Schedule initial refresh after the server starts**

```csharp
var serverTask = server.RunAsync(service.HandleAsync, cancellationToken);
_ = RefreshInitialSnapshotAsync(service, cancellationToken);
System.Windows.Forms.Application.Run(context);
```

Add `RefreshInitialSnapshotAsync` to consume expected shutdown cancellation without terminating the Host.

- [ ] **Step 4: Run the focused test**

Run: `dotnet test src/CalendarTimeline/tests/CalendarTimeline.Core.Tests --no-restore --filter "FullyQualifiedName~Task7ReviewFixTests"`

Expected: PASS.

### Task 4: Verify integrated behavior

**Files:**
- Modify: none

- [ ] **Step 1: Restore dependencies**

Run: `dotnet restore src/CalendarTimeline/CalendarTimeline.sln`

Expected: restore succeeds.

- [ ] **Step 2: Run the complete test suite**

Run: `dotnet test src/CalendarTimeline/CalendarTimeline.sln --no-restore`

Expected: PASS with zero failures.

- [ ] **Step 3: Build the Windows Host target**

Run: `dotnet build src/CalendarTimeline/src/CalendarTimeline.Host/CalendarTimeline.Host.csproj -f net10.0-windows10.0.19041.0 --no-restore`

Expected: build succeeds and co-deploys Windows Worker and WPF artifacts.
