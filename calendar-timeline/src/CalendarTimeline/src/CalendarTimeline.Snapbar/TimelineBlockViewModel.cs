namespace CalendarTimeline.Snapbar;

public sealed class TimelineBlockViewModel
{
    public TimelineBlockViewModel(
        string title,
        string subtitle,
        int lane,
        double startRatio,
        double widthRatio,
        bool isRunning,
        string? teamsUrl)
    {
        Title = title;
        Subtitle = subtitle;
        Lane = lane;
        StartRatio = startRatio;
        WidthRatio = widthRatio;
        IsRunning = isRunning;
        TeamsUrl = teamsUrl;
    }

    public string Title { get; }

    public string Subtitle { get; }

    public int Lane { get; }

    public double StartRatio { get; }

    public double WidthRatio { get; }

    public bool IsRunning { get; }

    public string? TeamsUrl { get; }

    public bool HasTeamsUrl => !string.IsNullOrWhiteSpace(TeamsUrl);
}
