using System.Diagnostics;
using CalendarTimeline.Core;

namespace CalendarTimeline.Host;

public sealed class WorkerHostSnapshotSource : IHostSnapshotSource
{
    private readonly string workerExecutablePath;

    public WorkerHostSnapshotSource()
        : this(ResolveWorkerExecutablePath())
    {
    }

    public WorkerHostSnapshotSource(string workerExecutablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerExecutablePath);
        this.workerExecutablePath = workerExecutablePath;
    }

    public async Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = workerExecutablePath,
            ArgumentList = { "--outlook-once" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Calendar worker could not be started.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Calendar worker failed." : error.Trim());
        }

        return CalendarSnapshotJson.Deserialize(output);
    }

    private static string ResolveWorkerExecutablePath()
    {
        var workerFileName = OperatingSystem.IsWindows() ? "CalendarTimeline.Worker.exe" : "CalendarTimeline.Worker";
        var directPath = Path.Combine(AppContext.BaseDirectory, workerFileName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var hostBaseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var directory = hostBaseDirectory; directory is not null; directory = directory.Parent)
        {
            if (!string.Equals(directory.Name, "CalendarTimeline.Host", StringComparison.Ordinal)
                || directory.Parent is null)
            {
                continue;
            }

            var hostBinDirectory = Path.Combine(directory.FullName, "bin");
            var relativeOutputDirectory = Path.GetRelativePath(hostBinDirectory, AppContext.BaseDirectory);
            if (relativeOutputDirectory.StartsWith("..", StringComparison.Ordinal))
            {
                break;
            }

            var workerPath = Path.Combine(
                directory.Parent.FullName,
                "CalendarTimeline.Worker",
                "bin",
                relativeOutputDirectory,
                workerFileName);
            if (File.Exists(workerPath))
            {
                return workerPath;
            }
        }

        return directPath;
    }
}
