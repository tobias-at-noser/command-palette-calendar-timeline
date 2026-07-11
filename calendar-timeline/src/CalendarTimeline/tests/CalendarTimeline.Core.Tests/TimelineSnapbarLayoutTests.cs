using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineSnapbarLayoutTests
{
    [Theory]
    [InlineData(1, 36)]
    [InlineData(3, 96)]
    public void GetTimelineHeight_ReservesSpaceForEveryBubbleLane(int laneCount, double expectedHeight)
    {
        Assert.Equal(expectedHeight, TimelineSnapbarLayout.GetTimelineHeight(laneCount));
    }

    [Fact]
    public void GetWindowHeightPreservesManualHeightAndOnlyExpandsForNewLanes()
    {
        Assert.Equal(96, TimelineSnapbarLayout.GetWindowHeight(96, 66));
        Assert.Equal(96, TimelineSnapbarLayout.GetWindowHeight(36, 96));
    }

    [Fact]
    public void GetBlockTop_StacksLanesAboveTheRailWithoutClipping()
    {
        const int laneCount = 3;

        Assert.Equal(60, TimelineSnapbarLayout.GetBlockTop(0, laneCount));
        Assert.Equal(30, TimelineSnapbarLayout.GetBlockTop(1, laneCount));
        Assert.Equal(0, TimelineSnapbarLayout.GetBlockTop(2, laneCount));
        Assert.True(
            TimelineSnapbarLayout.GetBlockTop(0, laneCount) + TimelineSnapbarLayout.BubbleHeight
            <= TimelineSnapbarLayout.GetTimelineHeight(laneCount) - TimelineSnapbarLayout.RailClearance);
    }

    [Fact]
    public void GetBlockTop_PlacesTheOnlyLaneAtTheTop()
    {
        Assert.Equal(0, TimelineSnapbarLayout.GetBlockTop(0, 1));
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
    public void NowRatio_MatchesTheConfiguredCalendarWindow()
    {
        Assert.Equal(1d / 9d, TimelineSnapbarLayout.NowRatio, 10);
    }

    [Fact]
    public void GetBlockBounds_ClampsAMinimumWidthBubbleAtTheRightEdge()
    {
        var bounds = TimelineSnapbarLayout.GetBlockBounds(100, 0.95, 0.01, 36);

        Assert.Equal(64, bounds.Left);
        Assert.Equal(36, bounds.Width);
        Assert.Equal(100, bounds.Left + bounds.Width);
    }

    [Fact]
    public void GetBlockBounds_FitsTheBubbleInsideANarrowTimeline()
    {
        var bounds = TimelineSnapbarLayout.GetBlockBounds(20, 0.8, 0.1, 36);

        Assert.Equal(0, bounds.Left);
        Assert.Equal(20, bounds.Width);
        Assert.Equal(20, bounds.Left + bounds.Width);
    }
}
