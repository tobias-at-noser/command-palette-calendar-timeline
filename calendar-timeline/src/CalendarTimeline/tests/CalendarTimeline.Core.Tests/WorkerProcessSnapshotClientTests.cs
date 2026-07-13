using CalendarTimeline.CommandPalette;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class WorkerProcessSnapshotClientTests
{
    [Fact]
    public async Task LoadSnapshotAsyncStartsWorkerAndParsesJsonSnapshot()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var tempDirectory = Directory.CreateTempSubdirectory();
        var workerPath = Path.Combine(tempDirectory.FullName, "fake-worker.sh");
        await File.WriteAllTextAsync(
            workerPath,
            "#!/bin/sh\nprintf '%s\n' '{\"generatedAt\":\"2026-07-10T10:00:00+00:00\",\"windowStart\":\"2026-07-10T09:30:00+00:00\",\"windowEnd\":\"2026-07-10T14:00:00+00:00\",\"appointments\":[{\"id\":\"from-worker\",\"title\":\"Termin\",\"location\":\"Raum\",\"start\":\"2026-07-10T10:30:00+00:00\",\"end\":\"2026-07-10T11:00:00+00:00\",\"isPrivate\":false,\"isConfidential\":false,\"teamsUrl\":null}],\"statusMessage\":null}'\n",
            TestContext.Current.CancellationToken);
        File.SetUnixFileMode(workerPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var client = new WorkerProcessSnapshotClient(workerPath);

        var snapshot = await client.LoadSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal("from-worker", snapshot.Appointments.Single().Id);
    }

    [Fact]
    public async Task LoadSnapshotAsyncThrowsWhenWorkerExitsWithError()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var tempDirectory = Directory.CreateTempSubdirectory();
        var workerPath = Path.Combine(tempDirectory.FullName, "failing-worker.sh");
        await File.WriteAllTextAsync(
            workerPath,
            "#!/bin/sh\nprintf '%s\n' 'Outlook-Kalender nicht verfügbar' >&2\nexit 2\n",
            TestContext.Current.CancellationToken);
        File.SetUnixFileMode(workerPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var client = new WorkerProcessSnapshotClient(workerPath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.LoadSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Outlook-Kalender nicht verfügbar", exception.Message);
    }
}
