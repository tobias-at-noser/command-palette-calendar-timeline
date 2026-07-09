using CalendarTimeline.Worker;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class WorkerCommandLineTests
{
    [Fact]
    public async Task FakeOnceReturnsSerializedFakeSnapshot()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await WorkerCommandLine.RunAsync(["--fake-once"], output, error, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"generatedAt\"", output.ToString());
        Assert.Contains("\"fake-running\"", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task OutlookOnceUsesOutlookSnapshotSource()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var source = new StubCalendarSnapshotSource("stub-outlook");
        var exitCode = await WorkerCommandLine.RunAsync(["--outlook-once"], output, error, TestContext.Current.CancellationToken, source);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"stub-outlook\"", output.ToString());
        Assert.Equal(1, source.CallCount);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task OutlookOnceReportsUnavailableWhenSourceFails()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var source = new FailingCalendarSnapshotSource();
        var exitCode = await WorkerCommandLine.RunAsync(["--outlook-once"], output, error, TestContext.Current.CancellationToken, source);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("Outlook-Kalender nicht verfügbar", error.ToString());
    }
}
