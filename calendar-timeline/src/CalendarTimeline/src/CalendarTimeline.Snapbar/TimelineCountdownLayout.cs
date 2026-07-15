namespace CalendarTimeline.Snapbar;

public readonly record struct TimelineHorizontalBounds(double Left, double Width)
{
    public double Right => Left + Width;
}

public static class TimelineCountdownLayout
{
    public static double GetLeft(
        double baseLeft,
        double indicatorWidth,
        double targetLeft,
        IEnumerable<TimelineHorizontalBounds> runningBlocks)
    {
        var afterRunningBlocks = runningBlocks
            .Where(block => block.Right > baseLeft)
            .Select(block => block.Right)
            .DefaultIfEmpty(baseLeft)
            .Max();
        return Math.Min(afterRunningBlocks, targetLeft - indicatorWidth);
    }
}
