namespace CalendarTimeline.Core;

public sealed record Appointment(
    string Id,
    string Title,
    string Location,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsPrivate,
    bool IsConfidential,
    string? TeamsUrl);
