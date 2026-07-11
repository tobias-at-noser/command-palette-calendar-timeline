using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class SnapbarWindowInteractionTests
{
    [Theory]
    [InlineData(0, 0, SnapbarResizeDirection.TopLeft)]
    [InlineData(100, 0, SnapbarResizeDirection.Top)]
    [InlineData(199, 0, SnapbarResizeDirection.TopRight)]
    [InlineData(199, 50, SnapbarResizeDirection.Right)]
    [InlineData(199, 99, SnapbarResizeDirection.BottomRight)]
    [InlineData(100, 99, SnapbarResizeDirection.Bottom)]
    [InlineData(0, 99, SnapbarResizeDirection.BottomLeft)]
    [InlineData(0, 50, SnapbarResizeDirection.Left)]
    [InlineData(100, 50, SnapbarResizeDirection.None)]
    public void GetResizeDirectionMapsEveryEdgeAndCorner(double x, double y, SnapbarResizeDirection expected)
    {
        Assert.Equal(expected, SnapbarWindowInteraction.GetResizeDirection(x, y, 200, 100, 8));
    }

    [Fact]
    public void DefaultResizeBorderExtendsFurtherInsideTheVisibleFrame()
    {
        Assert.Equal(
            SnapbarResizeDirection.Right,
            SnapbarWindowInteraction.GetResizeDirection(
                183,
                50,
                200,
                100,
                SnapbarWindowInteraction.DefaultResizeBorder));
    }

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(499, 97, true)]
    [InlineData(99, 50, false)]
    [InlineData(500, 50, false)]
    [InlineData(100, 98, false)]
    public void IsWithinBoundsUsesTheNativeWindowRectangle(int x, int y, bool expected)
    {
        Assert.Equal(expected, SnapbarWindowInteraction.IsWithinBounds(x, y, 100, 50, 500, 98));
    }

    [Fact]
    public void CanBeginDragRejectsAppointmentTargets()
    {
        Assert.False(SnapbarWindowInteraction.CanBeginDrag(true));
        Assert.True(SnapbarWindowInteraction.CanBeginDrag(false));
    }
}
