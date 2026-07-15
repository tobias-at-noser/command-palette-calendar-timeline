namespace CalendarTimeline.Snapbar;

public static class TimelineBubbleLayout
{
    // Includes the bubble chrome plus the compact start time, dot, and duration metadata.
    public const double DurationVisibleMinimumWidth = 96;

    public static bool ShouldShowDuration(double width)
    {
        return width >= DurationVisibleMinimumWidth;
    }
}
