using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace CalendarTimeline.Ipc;

public sealed class CalendarTimelinePipeServer
{
    private readonly string pipeName;
    private readonly Action? requestReadStarted;
    private readonly Func<Task>? responseWriteStarted;
    private readonly Action? clientFinished;

    public CalendarTimelinePipeServer(string? pipeName = null)
        : this(pipeName, null, null, null)
    {
    }

    internal CalendarTimelinePipeServer(string? pipeName, Action? requestReadStarted)
        : this(pipeName, requestReadStarted, null, null)
    {
    }

    internal CalendarTimelinePipeServer(string? pipeName, Action? requestReadStarted, Func<Task>? responseWriteStarted)
        : this(pipeName, requestReadStarted, responseWriteStarted, null)
    {
    }

    internal CalendarTimelinePipeServer(
        string? pipeName,
        Action? requestReadStarted,
        Func<Task>? responseWriteStarted,
        Action? clientFinished)
    {
        this.pipeName = string.IsNullOrWhiteSpace(pipeName) ? CalendarTimelinePipeNames.Default : pipeName;
        this.requestReadStarted = requestReadStarted;
        this.responseWriteStarted = responseWriteStarted;
        this.clientFinished = clientFinished;
    }

    public async Task RunAsync(Func<CalendarTimelineRequest, CancellationToken, Task<CalendarTimelineResponse>> handler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            try
            {
                await stream.WaitForConnectionAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

                string? requestLine;
                try
                {
                    var requestLineTask = reader.ReadLineAsync(cancellationToken).AsTask();
                    requestReadStarted?.Invoke();
                    requestLine = await requestLineTask;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (requestLine is null)
                {
                    continue;
                }

                string responseJson;

                try
                {
                    var request = CalendarTimelinePipeJson.DeserializeRequest(requestLine);
                    var response = await handler(request, cancellationToken);
                    responseJson = CalendarTimelinePipeJson.SerializeResponse(response);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    responseJson = CreateErrorResponseJson(requestLine, exception);
                }

                try
                {
                    if (responseWriteStarted is not null)
                    {
                        await responseWriteStarted();
                    }

                    await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken);
                    await writer.FlushAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (IOException)
                {
                    continue;
                }
            }
            catch (IOException)
            {
                continue;
            }
            finally
            {
                clientFinished?.Invoke();
            }
        }
    }

    public static string CreateErrorResponseJson(string? requestJson, Exception? exception)
    {
        var message = exception?.Message;

        if (string.IsNullOrWhiteSpace(message))
        {
            message = string.IsNullOrWhiteSpace(requestJson)
                ? "Calendar timeline pipe request failed."
                : "Calendar timeline pipe request contained invalid JSON.";
        }

        return CalendarTimelinePipeJson.SerializeResponse(new ErrorResponse(message));
    }
}
