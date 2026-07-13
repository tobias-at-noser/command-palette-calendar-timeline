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
        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new CalendarTimelinePipeServer(pipeName, readStarted.SetResult);
        using var cancellationSource = new CancellationTokenSource();
        var serverTask = server.RunAsync(
            (_, _) => Task.FromResult<CalendarTimelineResponse>(new StatusResponse("ok")),
            cancellationSource.Token);

        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        await client.ConnectAsync(TestContext.Current.CancellationToken);
        await client.WriteAsync(Encoding.UTF8.GetBytes("{"), TestContext.Current.CancellationToken);
        await client.FlushAsync(TestContext.Current.CancellationToken);
        await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        cancellationSource.Cancel();

        await serverTask;
    }

    [Fact]
    public async Task RunAsyncContinuesAfterClientDisconnectsBeforeResponseWrite()
    {
        var pipeName = $"calendar-timeline-test-{Guid.NewGuid():N}";
        var responseWriteStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowResponseWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstClientFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new CalendarTimelinePipeServer(
            pipeName,
            null,
            async () =>
            {
                responseWriteStarted.SetResult();
                await allowResponseWrite.Task;
            },
            firstClientFinished.SetResult);
        using var cancellationSource = new CancellationTokenSource();
        var serverTask = server.RunAsync(
            (_, _) => Task.FromResult<CalendarTimelineResponse>(new StatusResponse("ok")),
            cancellationSource.Token);

        await using (var disconnectedClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
        {
            await disconnectedClient.ConnectAsync(TestContext.Current.CancellationToken);
            await disconnectedClient.WriteAsync(
                Encoding.UTF8.GetBytes(CalendarTimelinePipeJson.SerializeRequest(new PingRequest()) + Environment.NewLine),
                TestContext.Current.CancellationToken);
            await disconnectedClient.FlushAsync(TestContext.Current.CancellationToken);
            await responseWriteStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        }

        allowResponseWrite.SetResult();
        await firstClientFinished.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.False(serverTask.IsCompleted);
        cancellationSource.Cancel();
        await serverTask;
    }
}
