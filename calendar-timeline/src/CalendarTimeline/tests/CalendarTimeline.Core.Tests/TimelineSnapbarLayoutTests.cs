using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineSnapbarLayoutTests
{
    [Theory]
    [InlineData(1, 42)]
    [InlineData(3, 114)]
    public void GetTimelineHeight_ReservesSpaceForEveryBubbleLane(int laneCount, double expectedHeight)
    {
        Assert.Equal(expectedHeight, TimelineSnapbarLayout.GetTimelineHeight(laneCount));
    }

    [Fact]
    public void GetWindowHeightPreservesManualHeightAndOnlyExpandsForNewLanes()
    {
        Assert.Equal(114, TimelineSnapbarLayout.GetWindowHeight(114, 78));
        Assert.Equal(114, TimelineSnapbarLayout.GetWindowHeight(42, 114));
    }

    [Fact]
    public void GetBlockTop_StacksLanesBelowLaneZeroWithoutClipping()
    {
        const int laneCount = 3;

        Assert.Equal(0, TimelineSnapbarLayout.GetBlockTop(0, laneCount));
        Assert.Equal(36, TimelineSnapbarLayout.GetBlockTop(1, laneCount));
        Assert.Equal(72, TimelineSnapbarLayout.GetBlockTop(2, laneCount));
        Assert.True(
            TimelineSnapbarLayout.GetBlockTop(0, laneCount) + TimelineSnapbarLayout.BubbleHeight
            <= TimelineSnapbarLayout.GetTimelineHeight(laneCount) - TimelineSnapbarLayout.RailClearance);
    }

    [Fact]
    public void GetBlockTop_PlacesTheOnlyLaneAtTheTop()
    {
        Assert.Equal(0, TimelineSnapbarLayout.GetBlockTop(0, 1));
    }

    [Fact]
    public void TimelineCenterAndNowLineBounds_AreLimitedToLaneZero()
    {
        var bounds = TimelineSnapbarLayout.GetNowLineBounds();

        Assert.Equal(52, TimelineSnapbarLayout.MinimumBlockWidth);
        Assert.Equal(16, TimelineSnapbarLayout.GetTimelineCenterY());
        Assert.Equal(TimelineSnapbarLayout.GetTimelineCenterY(), bounds.CenterY);
        Assert.Equal(TimelineSnapbarLayout.BubbleHeight, bounds.Height);
        Assert.Equal(0, bounds.Top);
        Assert.Equal(TimelineSnapbarLayout.BubbleHeight, bounds.Bottom);
        Assert.True(bounds.Bottom < TimelineSnapbarLayout.GetBlockTop(1, 2));
    }

    [Fact]
    public void GetRailBounds_CentersTheRailBehindTheLaneZeroBubble()
    {
        var bounds = TimelineSnapbarLayout.GetRailBounds();

        Assert.Equal(TimelineSnapbarLayout.RailHeight, bounds.Height);
        Assert.Equal(TimelineSnapbarLayout.GetTimelineCenterY(), bounds.CenterY);
        Assert.Equal(15, bounds.Top);
        Assert.Equal(17, bounds.Bottom);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void GetBlockTop_RejectsLanesOutsideTheLaneCount(int lane)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => TimelineSnapbarLayout.GetBlockTop(lane, 1));

        Assert.Equal("lane", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetBlockTop_RejectsNonPositiveLaneCounts(int laneCount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => TimelineSnapbarLayout.GetBlockTop(0, laneCount));

        Assert.Equal("laneCount", exception.ParamName);
    }

    [Fact]
    public void FadeRatios_MatchTheConfiguredCalendarWindow()
    {
        Assert.Equal(1d / 9d, TimelineSnapbarLayout.NowRatio, 10);
        Assert.Equal(TimelineSnapbarLayout.NowRatio, TimelineSnapbarLayout.FadeInEndRatio, 10);
        Assert.Equal(8d / 9d, TimelineSnapbarLayout.FadeOutStartRatio, 10);
        Assert.Equal(
            1d,
            TimelineSnapbarLayout.FadeInEndRatio + TimelineSnapbarLayout.FadeOutStartRatio,
            10);
    }

    [Fact]
    public void GetBlockBounds_PreservesMinimumWidthWhenEnteringFromTheRight()
    {
        var bounds = TimelineSnapbarLayout.GetBlockBounds(
            100,
            0.95,
            0.01,
            TimelineSnapbarLayout.MinimumBlockWidth);

        Assert.Equal(95, bounds.Left);
        Assert.Equal(52, bounds.Width);
    }

    [Fact]
    public void GetBlockBounds_PreservesMinimumWidthWhenLeavingToTheLeft()
    {
        var bounds = TimelineSnapbarLayout.GetBlockBounds(
            100,
            -0.10,
            0.20,
            TimelineSnapbarLayout.MinimumBlockWidth);

        Assert.Equal(-10, bounds.Left);
        Assert.Equal(52, bounds.Width);
    }

    [Fact]
    public void GetBlockBounds_PreservesBoundsForBlocksSpanningTheViewport()
    {
        var bounds = TimelineSnapbarLayout.GetBlockBounds(
            100,
            -0.20,
            1.40,
            TimelineSnapbarLayout.MinimumBlockWidth);

        Assert.Equal(-20, bounds.Left);
        Assert.Equal(140, bounds.Width);
    }
}
