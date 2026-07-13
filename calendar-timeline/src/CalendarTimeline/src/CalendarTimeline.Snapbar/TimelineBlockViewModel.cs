namespace CalendarTimeline.Snapbar;

public sealed class TimelineBlockViewModel
{
    public TimelineBlockViewModel(
        string title,
        string subtitle,
        string startTime,
        string tooltip,
        string calendarIdentity,
        string? calendarColor,
        IReadOnlyList<string?> categoryColors,
        int lane,
        double startRatio,
        double widthRatio,
        bool isRunning,
        string? teamsUrl,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        Title = title;
        Subtitle = subtitle;
        StartTime = startTime;
        Tooltip = tooltip;
        CalendarIdentity = calendarIdentity;
        CalendarColor = calendarColor;
        CategoryColors = categoryColors;
        Lane = lane;
        StartRatio = startRatio;
        WidthRatio = widthRatio;
        IsRunning = isRunning;
        TeamsUrl = teamsUrl;
        Start = start;
        End = end;
    }

    public string Title { get; }

    public string Subtitle { get; }

    public string StartTime { get; }

    public string Tooltip { get; }

    public string CalendarIdentity { get; }

    public string? CalendarColor { get; }

    public IReadOnlyList<string?> CategoryColors { get; }

    public int Lane { get; }

    public double StartRatio { get; }

    public double WidthRatio { get; }

    public bool IsRunning { get; }

    public string? TeamsUrl { get; }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public bool HasTeamsUrl => !string.IsNullOrWhiteSpace(TeamsUrl);
}
