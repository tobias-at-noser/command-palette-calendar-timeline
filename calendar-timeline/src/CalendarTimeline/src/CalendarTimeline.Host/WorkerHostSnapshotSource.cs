using System.Diagnostics;
using CalendarTimeline.Core;

namespace CalendarTimeline.Host;

public sealed class WorkerHostSnapshotSource : IHostSnapshotSource
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private readonly string workerExecutablePath;
    private readonly bool launchesViaDotnet;
    private readonly TimeSpan timeout;

    public WorkerHostSnapshotSource()
        : this(ResolveWorkerExecutablePath())
    {
    }

    public WorkerHostSnapshotSource(string workerExecutablePath)
        : this(workerExecutablePath, DefaultTimeout)
    {
    }

    public WorkerHostSnapshotSource(string workerExecutablePath, TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerExecutablePath);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        this.workerExecutablePath = workerExecutablePath;
        launchesViaDotnet = string.Equals(Path.GetExtension(workerExecutablePath), ".dll", StringComparison.OrdinalIgnoreCase);
        this.timeout = timeout;
    }

    public async Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var executionSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var executionToken = executionSource.Token;
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = launchesViaDotnet ? "dotnet" : workerExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(workerExecutablePath) ?? AppContext.BaseDirectory,
        };
        if (launchesViaDotnet)
        {
            process.StartInfo.ArgumentList.Add(workerExecutablePath);
        }

        process.StartInfo.ArgumentList.Add("--outlook-once");

        var hasStarted = false;
        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Calendar worker could not be started.");
            }

            hasStarted = true;
            var outputTask = process.StandardOutput.ReadToEndAsync(executionToken);
            var errorTask = process.StandardError.ReadToEndAsync(executionToken);
            await process.WaitForExitAsync(executionToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Calendar worker failed." : error.Trim());
            }

            return CalendarSnapshotJson.Deserialize(output);
        }
        catch (OperationCanceledException exception) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            if (hasStarted)
            {
                await StopProcessAsync(process);
            }

            throw new TimeoutException("Calendar worker timed out.", exception);
        }
        catch
        {
            if (hasStarted)
            {
                await StopProcessAsync(process);
            }

            throw;
        }
    }

    private static async Task StopProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The worker exited between the status check and the kill request.
        }

        await process.WaitForExitAsync(CancellationToken.None);
    }

    private static string ResolveWorkerExecutablePath()
    {
        return ResolveWorkerExecutablePath(AppContext.BaseDirectory);
    }

    private static string ResolveWorkerExecutablePath(string hostBaseDirectory)
    {
        var directPath = FindWorkerArtifactPath(hostBaseDirectory);
        if (directPath is not null)
        {
            return directPath;
        }

        var baseDirectory = new DirectoryInfo(hostBaseDirectory);
        for (var directory = baseDirectory; directory is not null; directory = directory.Parent)
        {
            if (!string.Equals(directory.Name, "CalendarTimeline.Host", StringComparison.Ordinal)
                || directory.Parent is null)
            {
                continue;
            }

            var hostBinDirectory = Path.Combine(directory.FullName, "bin");
            var relativeOutputDirectory = Path.GetRelativePath(hostBinDirectory, hostBaseDirectory);
            if (relativeOutputDirectory.StartsWith("..", StringComparison.Ordinal))
            {
                break;
            }

            var workerOutputDirectory = Path.Combine(
                directory.Parent.FullName,
                "CalendarTimeline.Worker",
                "bin",
                relativeOutputDirectory);
            var workerPath = FindWorkerArtifactPath(workerOutputDirectory);
            if (workerPath is not null)
            {
                return workerPath;
            }
        }

        return Path.Combine(hostBaseDirectory, GetWorkerArtifactNames()[0]);
    }

    private static string? FindWorkerArtifactPath(string directory)
    {
        var workerDllPath = Path.Combine(directory, "CalendarTimeline.Worker.dll");
        if (!File.Exists(workerDllPath)
            || !File.Exists(Path.Combine(directory, "CalendarTimeline.Worker.deps.json"))
            || !File.Exists(Path.Combine(directory, "CalendarTimeline.Worker.runtimeconfig.json"))
            || !File.Exists(Path.Combine(directory, "CalendarTimeline.Core.dll")))
        {
            return null;
        }

        var appHostPath = Path.Combine(directory, GetWorkerArtifactNames()[0]);
        return File.Exists(appHostPath) ? appHostPath : workerDllPath;
    }

    private static string[] GetWorkerArtifactNames()
    {
        return OperatingSystem.IsWindows()
            ? ["CalendarTimeline.Worker.exe", "CalendarTimeline.Worker.dll"]
            : ["CalendarTimeline.Worker", "CalendarTimeline.Worker.dll"];
    }
}
