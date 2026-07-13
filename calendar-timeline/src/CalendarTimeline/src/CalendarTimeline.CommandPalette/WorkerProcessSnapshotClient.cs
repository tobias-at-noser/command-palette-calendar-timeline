using System.Diagnostics;
using CalendarTimeline.Core;

namespace CalendarTimeline.CommandPalette;

public sealed class WorkerProcessSnapshotClient : IWorkerSnapshotClient
{
    private readonly string workerExecutablePath;

    public WorkerProcessSnapshotClient()
        : this(Path.Combine(AppContext.BaseDirectory, GetWorkerExecutableName()))
    {
    }

    public WorkerProcessSnapshotClient(string workerExecutablePath)
    {
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

    private static string GetWorkerExecutableName()
    {
        return OperatingSystem.IsWindows() ? "CalendarTimeline.Worker.exe" : "CalendarTimeline.Worker";
    }
}
