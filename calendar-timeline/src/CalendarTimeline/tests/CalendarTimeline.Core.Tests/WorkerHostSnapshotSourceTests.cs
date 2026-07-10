using CalendarTimeline.Host;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class WorkerHostSnapshotSourceTests
{
    [Fact]
    public async Task LoadSnapshotAsyncStartsWorkerAndDeserializesSnapshot()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var workerPath = await CreateExecutableWorkerAsync(
            "[ \"$1\" = \"--outlook-once\" ] || exit 3\nprintf '%s\\n' '{\"generatedAt\":\"2026-07-10T10:00:00+00:00\",\"windowStart\":\"2026-07-10T09:30:00+00:00\",\"windowEnd\":\"2026-07-10T14:00:00+00:00\",\"appointments\":[],\"statusMessage\":null}'");
        var source = new WorkerHostSnapshotSource(workerPath);

        var snapshot = await source.LoadSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero), snapshot.GeneratedAt);
    }

    [Fact]
    public async Task LoadSnapshotAsyncIncludesWorkerErrorWhenWorkerFails()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var source = new WorkerHostSnapshotSource(await CreateExecutableWorkerAsync(
            "printf '%s\\n' 'Outlook-Kalender nicht verfügbar' >&2\nexit 2"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.LoadSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Outlook-Kalender nicht verfügbar", exception.Message);
    }

    private static async Task<string> CreateExecutableWorkerAsync(string command)
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException();
        }

        var tempDirectory = Directory.CreateTempSubdirectory();
        var workerPath = Path.Combine(tempDirectory.FullName, "fake-worker.sh");
        await File.WriteAllTextAsync(
            workerPath,
            $"#!/bin/sh\n{command}\n",
            TestContext.Current.CancellationToken);
        File.SetUnixFileMode(workerPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return workerPath;
    }
}
