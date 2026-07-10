using System.IO.Pipes;
using System.Text;
using CalendarTimeline.Ipc;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelinePipeServerTests
{
    [Fact]
    public async Task RunAsyncCompletesNormallyWhenShutdownCancelsAnIncompleteRequest()
    {
        var pipeName = $"calendar-timeline-test-{Guid.NewGuid():N}";
        var server = new CalendarTimelinePipeServer(pipeName);
        using var cancellationSource = new CancellationTokenSource();
        var serverTask = server.RunAsync(
            (_, _) => Task.FromResult<CalendarTimelineResponse>(new StatusResponse("ok")),
            cancellationSource.Token);

        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        await client.WriteAsync(Encoding.UTF8.GetBytes("{"), TestContext.Current.CancellationToken);
        await client.FlushAsync(TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);

        cancellationSource.Cancel();

        await serverTask;
    }
}
