namespace CalendarTimeline.Snapbar;

public static class TimelineSnapbarLayout
{
    public const double BubbleHeight = 28;
    public const double LanePitch = 30;
    public const double RailClearance = 6;
    public const double RailHeight = 2;
    public const double NowRatio = 1d / 9d;

    public static double GetTimelineHeight(int laneCount)
    {
        return Math.Max(1, laneCount) * LanePitch + RailClearance;
    }

    public static double GetBlockTop(int lane, int laneCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(laneCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(lane);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lane, laneCount);
        return (laneCount - lane - 1) * LanePitch;
    }
}
