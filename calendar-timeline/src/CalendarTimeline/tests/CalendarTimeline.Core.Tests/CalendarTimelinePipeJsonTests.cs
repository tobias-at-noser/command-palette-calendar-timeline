using CalendarTimeline.Core;
using CalendarTimeline.Ipc;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelinePipeJsonTests
{
    [Fact]
    public void SerializeAndDeserializeSnapshotResponse_RoundTripsSnapshot()
    {
        var now = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
        var response = new SnapshotResponse(new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4), [], "ok"));

        var json = CalendarTimelinePipeJson.SerializeResponse(response);
        var roundTripped = CalendarTimelinePipeJson.DeserializeResponse(json);

        var snapshotResponse = Assert.IsType<SnapshotResponse>(roundTripped);
        Assert.Equal("ok", snapshotResponse.Snapshot.StatusMessage);
    }

    [Fact]
    public void SerializeAndDeserializePingRequest_RoundTripsRequest()
    {
        var json = CalendarTimelinePipeJson.SerializeRequest(new PingRequest());
        var roundTripped = CalendarTimelinePipeJson.DeserializeRequest(json);

        Assert.IsType<PingRequest>(roundTripped);
    }

    [Fact]
    public void SerializeAndDeserializeErrorResponse_RoundTripsMessage()
    {
        var json = CalendarTimelinePipeJson.SerializeResponse(new ErrorResponse("boom"));
        var roundTripped = CalendarTimelinePipeJson.DeserializeResponse(json);

        var error = Assert.IsType<ErrorResponse>(roundTripped);
        Assert.Equal("boom", error.Message);
    }

    [Fact]
    public void FormatPipeNameForUser_SanitizesInvalidCharactersAndAppendsDeterministicSuffix()
    {
        var first = CalendarTimelinePipeNames.FormatPipeNameForUser("a:b");
        var second = CalendarTimelinePipeNames.FormatPipeNameForUser("a?b");

        Assert.StartsWith("calendar-timeline-ab-", first);
        Assert.StartsWith("calendar-timeline-ab-", second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CreateErrorResponseJson_ForMalformedRequest_ReturnsSingleLineErrorJson()
    {
        var json = CalendarTimelinePipeServer.CreateErrorResponseJson("{", null);
        var response = Assert.IsType<ErrorResponse>(CalendarTimelinePipeJson.DeserializeResponse(json));

        Assert.DoesNotContain('\n', json);
        Assert.DoesNotContain('\r', json);
        Assert.NotEmpty(response.Message);
    }

    [Fact]
    public void CreateErrorResponseJson_ForHandlerException_ReturnsSingleLineErrorJson()
    {
        var json = CalendarTimelinePipeServer.CreateErrorResponseJson(null, new InvalidOperationException("boom"));
        var response = Assert.IsType<ErrorResponse>(CalendarTimelinePipeJson.DeserializeResponse(json));

        Assert.DoesNotContain('\n', json);
        Assert.DoesNotContain('\r', json);
        Assert.Equal("boom", response.Message);
    }
}
