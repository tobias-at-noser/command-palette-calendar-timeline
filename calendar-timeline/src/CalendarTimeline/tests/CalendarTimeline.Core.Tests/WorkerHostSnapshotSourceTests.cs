using System.Diagnostics;
using System.Reflection;
using CalendarTimeline.Host;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class WorkerHostSnapshotSourceTests
{
    [Fact]
    public async Task LoadSnapshotAsyncStartsWorkerAndDeserializesSnapshot()
    {
        if (!OperatingSystem.IsLinux())
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
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var source = new WorkerHostSnapshotSource(await CreateExecutableWorkerAsync(
            "printf '%s\\n' 'Outlook-Kalender nicht verfügbar' >&2\nexit 2"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.LoadSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Outlook-Kalender nicht verfügbar", exception.Message);
    }

    [Fact]
    public async Task LoadSnapshotAsyncCancelsAndWaitsForTheWorkerProcess()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var workerPath = await CreateExecutableWorkerAsync("printf '%s\\n' \"$$\" > \"$0.pid\"\nsleep 30");
        var processIdPath = workerPath + ".pid";
        var source = new WorkerHostSnapshotSource(workerPath);
        using var cancellationSource = new CancellationTokenSource();

        var loadTask = source.LoadSnapshotAsync(cancellationSource.Token);
        await WaitForFileAsync(processIdPath);
        cancellationSource.Cancel();

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loadTask);

            var processId = int.Parse(await File.ReadAllTextAsync(processIdPath, TestContext.Current.CancellationToken));
            Assert.False(IsProcessRunning(processId));
        }
        finally
        {
            if (File.Exists(processIdPath))
            {
                var processId = int.Parse(await File.ReadAllTextAsync(processIdPath, TestContext.Current.CancellationToken));
                TerminateProcess(processId);
            }
        }
    }

    [Theory]
    [InlineData("net10.0")]
    [InlineData("net10.0", "linux-musl-x64")]
    public void ResolveWorkerExecutablePathFindsSiblingWorkerDllWithoutAnAppHost(string targetFramework, string? runtimeIdentifier = null)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string[] outputPath = runtimeIdentifier is null
            ? ["Debug", targetFramework]
            : ["Debug", targetFramework, runtimeIdentifier];
        var hostOutputDirectory = Path.Combine([rootDirectory, "src", "CalendarTimeline.Host", "bin", .. outputPath]);
        var workerPath = Path.Combine([rootDirectory, "src", "CalendarTimeline.Worker", "bin", .. outputPath, "CalendarTimeline.Worker.dll"]);

        try
        {
            Directory.CreateDirectory(hostOutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(workerPath)!);
            File.WriteAllText(workerPath, string.Empty);

            Assert.Equal(workerPath, ResolveWorkerExecutablePath(hostOutputDirectory));
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    [Fact]
    public void ResolveWorkerExecutablePathFindsCoDeployedWorkerDll()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var workerPath = Path.Combine(outputDirectory, "CalendarTimeline.Worker.dll");

        try
        {
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(workerPath, string.Empty);

            Assert.Equal(workerPath, ResolveWorkerExecutablePath(outputDirectory));
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
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

    private static string ResolveWorkerExecutablePath(string hostBaseDirectory)
    {
        var method = typeof(WorkerHostSnapshotSource).GetMethod(
            "ResolveWorkerExecutablePath",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);
        Assert.NotNull(method);
        return Assert.IsType<string>(method.Invoke(null, [hostBaseDirectory]));
    }

    private static async Task WaitForFileAsync(string path)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (File.Exists(path))
            {
                return;
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"Worker did not create '{path}'.");
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void TerminateProcess(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch (ArgumentException)
        {
        }
    }
}
