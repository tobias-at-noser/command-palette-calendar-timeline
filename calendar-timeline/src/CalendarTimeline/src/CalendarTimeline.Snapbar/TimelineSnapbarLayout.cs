namespace CalendarTimeline.Snapbar;

public static class TimelineSnapbarLayout
{
    public const double BubbleHeight = 32;
    public const double LanePitch = 36;
    public const double RailClearance = 6;
    public const double RailHeight = 2;
    public const double MinimumBlockWidth = 52;
    public const double NowRatio = 1d / 9d;
    public const double FadeInEndRatio = NowRatio;
    public const double FadeOutStartRatio = 1d - NowRatio;

    public static double GetTimelineHeight(int laneCount)
    {
        return Math.Max(1, laneCount) * LanePitch + RailClearance;
    }

    public static double GetWindowHeight(double currentHeight, double requiredHeight)
    {
        return Math.Max(currentHeight, requiredHeight);
    }

    public static double GetBlockTop(int lane, int laneCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(laneCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(lane);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lane, laneCount);
        return lane * LanePitch;
    }

    public static double GetAllDayTagTop(int laneCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(laneCount, 1);
        return BubbleHeight - AllDayTagLayout.TagHeight;
    }

    public static double GetTimelineCenterY()
    {
        return BubbleHeight / 2;
    }

    public static TimelineVerticalBounds GetNowLineBounds()
    {
        return new TimelineVerticalBounds(0, BubbleHeight);
    }

    public static TimelineVerticalBounds GetRailBounds()
    {
        return new TimelineVerticalBounds(
            GetTimelineCenterY() - (RailHeight / 2),
            RailHeight);
    }

    public static (double Left, double Width) GetBlockBounds(
        double timelineWidth,
        double startRatio,
        double widthRatio,
        double minimumWidth)
    {
        timelineWidth = Math.Max(0, timelineWidth);
        var left = timelineWidth * startRatio;
        var width = Math.Max(minimumWidth, timelineWidth * widthRatio);
        return (left, width);
    }
}

public readonly record struct TimelineVerticalBounds(double Top, double Height)
{
    public double Bottom => Top + Height;

    public double CenterY => Top + (Height / 2);
}
