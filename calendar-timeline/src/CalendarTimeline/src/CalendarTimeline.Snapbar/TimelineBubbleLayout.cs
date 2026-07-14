namespace CalendarTimeline.Snapbar;

public static class TimelineBubbleLayout
{
    public const double DurationVisibleMinimumWidth = 142;

    public static bool ShouldShowDuration(double width)
    {
        return width >= DurationVisibleMinimumWidth;
    }
}
