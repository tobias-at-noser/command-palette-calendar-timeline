namespace CalendarTimeline.Core;

public sealed record DockAgendaItem(
    string Title,
    string Subtitle,
    int Lane,
    bool IsRunning,
    string? TeamsUrl,
    string? AppointmentId);
