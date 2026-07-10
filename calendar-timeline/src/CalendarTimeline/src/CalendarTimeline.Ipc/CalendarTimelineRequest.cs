namespace CalendarTimeline.Ipc;

public abstract record CalendarTimelineRequest(string Type);

public sealed record PingRequest() : CalendarTimelineRequest("ping");

public sealed record GetSnapshotRequest() : CalendarTimelineRequest("getSnapshot");

public sealed record RefreshSnapshotRequest() : CalendarTimelineRequest("refreshSnapshot");
