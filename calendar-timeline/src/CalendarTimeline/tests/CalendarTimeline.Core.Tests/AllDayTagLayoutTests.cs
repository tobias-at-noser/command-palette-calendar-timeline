using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class AllDayTagLayoutTests
{
    private const double TimelineWidth = 900;
    private const double NowRatio = 1d / 9d;

    [Fact]
    public void GetBounds_FollowsStartBeforeItReachesNow()
    {
        const double startRatio = 0.2;

        var bounds = AllDayTagLayout.GetBounds(TimelineWidth, NowRatio, startRatio, 0.9);

        Assert.Equal(TimelineWidth * startRatio, bounds.Left);
        Assert.Equal(AllDayTagLayout.TagWidth, bounds.Width);
    }

    [Fact]
    public void GetBounds_ParksRightOfNowWhileEndReachesTheParkedRightEdge()
    {
        var nowX = TimelineWidth * NowRatio;
        var parkedRight = nowX + AllDayTagLayout.GapFromNowLine + AllDayTagLayout.TagWidth;

        var bounds = AllDayTagLayout.GetBounds(
            TimelineWidth,
            NowRatio,
            NowRatio,
            parkedRight / TimelineWidth);

        Assert.Equal(nowX + AllDayTagLayout.GapFromNowLine, bounds.Left);
    }

    [Fact]
    public void GetBounds_HandoffsContinuouslyAtTheParkedRightEdge()
    {
        var nowX = TimelineWidth * NowRatio;
        var parkedRight = nowX + AllDayTagLayout.GapFromNowLine + AllDayTagLayout.TagWidth;
        var parked = AllDayTagLayout.GetBounds(
            TimelineWidth,
            NowRatio,
            NowRatio,
            parkedRight / TimelineWidth);
        var exiting = AllDayTagLayout.GetBounds(
            TimelineWidth,
            NowRatio,
            NowRatio,
            (parkedRight - 0.1) / TimelineWidth);
        var endX = parkedRight;
        var exitFormulaAtBoundary = endX - AllDayTagLayout.TagWidth;

        Assert.Equal(parked.Left, exitFormulaAtBoundary);
        Assert.Equal(parkedRight - 0.1 - AllDayTagLayout.TagWidth, exiting.Left);
    }

    [Fact]
    public void GetBounds_FollowsEndAfterItPassesTheParkedRightEdge()
    {
        const double endRatio = 0.2;

        var bounds = AllDayTagLayout.GetBounds(TimelineWidth, NowRatio, NowRatio, endRatio);

        Assert.Equal((TimelineWidth * endRatio) - AllDayTagLayout.TagWidth, bounds.Left);
    }

    [Fact]
    public void GetAllDayTagTop_BottomAlignsWithinLaneZeroRegardlessOfLaneCount()
    {
        Assert.Equal(
            TimelineSnapbarLayout.BubbleHeight - AllDayTagLayout.TagHeight,
            TimelineSnapbarLayout.GetAllDayTagTop(3));
    }
}
