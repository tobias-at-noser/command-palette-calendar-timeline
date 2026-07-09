namespace CalendarTimeline.Core;

public sealed record CalendarSnapshot(
    DateTimeOffset GeneratedAt,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    IReadOnlyList<Appointment> Appointments,
    string? StatusMessage);
