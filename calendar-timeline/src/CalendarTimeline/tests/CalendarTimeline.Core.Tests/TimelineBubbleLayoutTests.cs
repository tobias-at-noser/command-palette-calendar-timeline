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
    public void ShouldShowDuration_ShowsDurationWhenTheCompactMetadataFits()
    {
        Assert.True(TimelineBubbleLayout.ShouldShowDuration(96));
    }

    [Fact]
    public void ShouldShowDuration_HidesDurationImmediatelyBelowTheCompactMetadataWidth()
    {
        Assert.False(TimelineBubbleLayout.ShouldShowDuration(95));
    }
}
