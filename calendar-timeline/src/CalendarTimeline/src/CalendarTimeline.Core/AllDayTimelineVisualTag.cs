namespace CalendarTimeline.Core;

public sealed record AllDayTimelineVisualTag(
    Appointment Appointment,
    string DisplayTitle,
    string? CalendarColor,
    IReadOnlyList<string?> CategoryColors);
