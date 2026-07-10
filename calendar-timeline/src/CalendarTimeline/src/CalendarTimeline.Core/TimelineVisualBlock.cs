namespace CalendarTimeline.Core;

public sealed record TimelineVisualBlock(
    Appointment Appointment,
    int Lane,
    double StartRatio,
    double EndRatio,
    bool IsRunning,
    string DisplayTitle,
    string DisplaySubtitle);
