using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public sealed record OutlookAppointmentData(
    string Id,
    string Title,
    string Location,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsPrivate,
    bool IsConfidential,
    string? Body,
    string CalendarId = "",
    string CalendarName = "",
    string? CalendarColor = null,
    IReadOnlyList<CalendarCategory>? Categories = null,
    bool IsAllDayEvent = false);
