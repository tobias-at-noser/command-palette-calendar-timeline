namespace CalendarTimeline.CommandPalette;

public sealed record TimelineRow(
    string Title,
    string Subtitle,
    int Lane,
    bool IsRunning,
    string? TeamsUrl)
{
    public bool HasTeamsLink => !string.IsNullOrWhiteSpace(TeamsUrl);
}
