using CalendarTimeline.Core;
using CalendarTimeline.Ipc;

namespace CalendarTimeline.Host;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cache = new HostSnapshotCache();

        if (args.Contains("--fake-once", StringComparer.Ordinal))
        {
            var snapshot = await new FakeHostSnapshotSource().LoadSnapshotAsync(CancellationToken.None);
            Console.WriteLine(CalendarSnapshotJson.Serialize(snapshot));
            return 0;
        }

        var service = new CalendarTimelineHostService(cache, new WorkerHostSnapshotSource());
        var server = new CalendarTimelinePipeServer();

        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        if (OperatingSystem.IsWindows())
        {
            await RunWindowsHostAsync(service, server, cancellationSource);
            return 0;
        }

        var serverTask = server.RunAsync(service.HandleAsync, cancellationSource.Token);
        _ = RefreshInitialSnapshotAsync(service, cancellationSource.Token);
        await AwaitServerShutdownAsync(serverTask, cancellationSource.Token);
        return 0;
    }

    private static async Task RunWindowsHostAsync(CalendarTimelineHostService service, CalendarTimelinePipeServer server, CancellationTokenSource cancellationSource)
    {
        var cancellationToken = cancellationSource.Token;
#if WINDOWS
        using var context = new TrayApplicationContext(service, cancellationToken, cancellationSource.Cancel);
        using var cancellationRegistration = cancellationToken.Register(context.ExitThreadSafely);
        var serverTask = server.RunAsync(service.HandleAsync, cancellationToken);
        _ = RefreshInitialSnapshotAsync(service, cancellationToken);
        _ = serverTask.ContinueWith(
            _ => cancellationSource.Cancel(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        try
        {
            System.Windows.Forms.Application.Run(context);
        }
        finally
        {
            cancellationSource.Cancel();
        }

        await AwaitServerShutdownAsync(serverTask, cancellationToken);
#else
        var serverTask = server.RunAsync(service.HandleAsync, cancellationToken);
        await AwaitServerShutdownAsync(serverTask, cancellationToken);
#endif
    }

    private static async Task AwaitServerShutdownAsync(Task serverTask, CancellationToken cancellationToken)
    {
        var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

        if (await Task.WhenAny(serverTask, cancellationTask) == serverTask)
        {
            await serverTask;
        }
    }

    private static async Task RefreshInitialSnapshotAsync(CalendarTimelineHostService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.HandleAsync(new RefreshSnapshotRequest(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
