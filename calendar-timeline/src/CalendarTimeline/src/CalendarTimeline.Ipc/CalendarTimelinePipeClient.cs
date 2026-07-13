using System.IO.Pipes;
using System.Text;

namespace CalendarTimeline.Ipc;

public sealed class CalendarTimelinePipeClient
{
    private readonly string pipeName;

    public CalendarTimelinePipeClient(string? pipeName = null)
    {
        this.pipeName = string.IsNullOrWhiteSpace(pipeName) ? CalendarTimelinePipeNames.Default : pipeName;
    }

    public async Task<CalendarTimelineResponse> SendAsync(CalendarTimelineRequest request, CancellationToken cancellationToken)
    {
        await using var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await stream.ConnectAsync(cancellationToken);

        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(CalendarTimelinePipeJson.SerializeRequest(request));
        await writer.FlushAsync(cancellationToken);

        var responseLine = await reader.ReadLineAsync(cancellationToken)
            ?? throw new IOException("Calendar timeline pipe server closed without a response.");

        return CalendarTimelinePipeJson.DeserializeResponse(responseLine);
    }
}
