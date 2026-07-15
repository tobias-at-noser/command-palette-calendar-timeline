using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineCountdownLayoutTests
{
    [Fact]
    public void GetLeft_UsesTheBasePositionWithoutRunningBlocks()
    {
        Assert.Equal(32, TimelineCountdownLayout.GetLeft(32, 20, 160, []));
    }

    [Fact]
    public void GetLeft_MovesBehindARunningBlock()
    {
        Assert.Equal(108, TimelineCountdownLayout.GetLeft(32, 20, 160, [new TimelineHorizontalBounds(20, 88)]));
    }

    [Fact]
    public void GetLeft_DoesNotMoveForARunningBlockBeyondTheIndicator()
    {
        Assert.Equal(32, TimelineCountdownLayout.GetLeft(32, 20, 160, [new TimelineHorizontalBounds(70, 20)]));
    }

    [Fact]
    public void GetLeft_MovesPastAChainOfOverlappingRunningBlocks()
    {
        Assert.Equal(
            74,
            TimelineCountdownLayout.GetLeft(
                32,
                20,
                160,
                [new TimelineHorizontalBounds(20, 30), new TimelineHorizontalBounds(49, 25)]));
    }

    [Fact]
    public void GetLeft_StopsBeforeTheTarget()
    {
        Assert.Equal(140, TimelineCountdownLayout.GetLeft(32, 20, 160, [new TimelineHorizontalBounds(20, 150)]));
    }
}
