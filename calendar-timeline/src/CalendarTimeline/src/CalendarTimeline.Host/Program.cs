using CalendarTimeline.Core;
using CalendarTimeline.Ipc;

namespace CalendarTimeline.Host;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cache = new HostSnapshotCache();
        var snapshotSource = new FakeHostSnapshotSource();

        if (args.Contains("--fake-once", StringComparer.Ordinal))
        {
            var snapshot = await snapshotSource.LoadSnapshotAsync(DateTimeOffset.Now, CancellationToken.None);
            Console.WriteLine(CalendarSnapshotJson.Serialize(snapshot));
            return 0;
        }

        var service = new CalendarTimelineHostService(cache, snapshotSource);
        var server = new CalendarTimelinePipeServer();

        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        await service.HandleAsync(new RefreshSnapshotRequest(), cancellationSource.Token);

        if (OperatingSystem.IsWindows())
        {
            await RunWindowsHostAsync(service, server, cancellationSource);
            return 0;
        }

        await server.RunAsync(service.HandleAsync, cancellationSource.Token);
        return 0;
    }

    private static async Task RunWindowsHostAsync(CalendarTimelineHostService service, CalendarTimelinePipeServer server, CancellationTokenSource cancellationSource)
    {
        var cancellationToken = cancellationSource.Token;
#if WINDOWS
        using var context = new TrayApplicationContext(service, cancellationToken, cancellationSource.Cancel);
        var serverTask = server.RunAsync(service.HandleAsync, cancellationToken);
        System.Windows.Forms.Application.Run(context);
        await serverTask;
#else
        await server.RunAsync(service.HandleAsync, cancellationToken);
#endif
    }
}
