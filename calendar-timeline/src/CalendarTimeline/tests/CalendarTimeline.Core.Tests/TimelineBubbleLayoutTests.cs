using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineBubbleLayoutTests
{
    [Fact]
    public void ShouldShowDuration_HidesDurationAtTheMinimumBubbleWidth()
    {
        Assert.False(TimelineBubbleLayout.ShouldShowDuration(TimelineSnapbarLayout.MinimumBlockWidth));
    }

    [Fact]
    public void ShouldShowDuration_ShowsDurationAtTheVisibilityThreshold()
    {
        Assert.True(TimelineBubbleLayout.ShouldShowDuration(TimelineBubbleLayout.DurationVisibleMinimumWidth));
    }
}
