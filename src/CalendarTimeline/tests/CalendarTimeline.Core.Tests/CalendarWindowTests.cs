using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarWindowTests
{
    [Fact]
    public void CreateUsesConfiguredRelativeWindow()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);

        var window = CalendarWindow.Create(now);

        Assert.Equal(now.AddMinutes(-30), window.Start);
        Assert.Equal(now.AddHours(4), window.End);
    }
}
