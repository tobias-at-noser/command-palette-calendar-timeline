using CalendarTimeline.Core;

namespace CalendarTimeline.Ipc;

public abstract record CalendarTimelineResponse(string Type);

public sealed record StatusResponse(string Status) : CalendarTimelineResponse("status");

public sealed record SnapshotResponse(CalendarSnapshot Snapshot) : CalendarTimelineResponse("snapshot");

public sealed record ErrorResponse(string Message) : CalendarTimelineResponse("error");
