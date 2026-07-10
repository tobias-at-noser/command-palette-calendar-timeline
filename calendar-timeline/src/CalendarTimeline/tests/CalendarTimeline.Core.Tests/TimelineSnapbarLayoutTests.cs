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
    public void NowRatio_MatchesTheConfiguredCalendarWindow()
    {
        Assert.Equal(1d / 9d, TimelineSnapbarLayout.NowRatio, 10);
    }
}
