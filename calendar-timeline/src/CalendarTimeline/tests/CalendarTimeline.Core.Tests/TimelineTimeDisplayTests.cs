using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineTimeDisplayTests
{
    [Fact]
    public void GetCountdown_RoundsTheNextAppointmentToFiveMinutes()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(now, [CreateBlock(now.AddMinutes(82), now.AddMinutes(112))]);

        Assert.Equal("01:20", result);
    }

    [Fact]
    public void GetCountdown_HidesTheCountdownWhileAnAppointmentIsRunning()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(now, [CreateBlock(now.AddMinutes(-10), now.AddMinutes(10))]);

        Assert.Null(result);
    }

    [Fact]
    public void GetCountdown_HidesTheCountdownWithoutAFutureAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(now, []);

        Assert.Null(result);
    }

    [Fact]
    public void GetDateTooltip_UsesTheGermanLongDateFormat()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal("Montag, 13.07.2026", TimelineTimeDisplay.GetDateTooltip(now));
    }

    private static TimelineBlockViewModel CreateBlock(DateTimeOffset start, DateTimeOffset end)
    {
        return new TimelineBlockViewModel(
            "Test",
            string.Empty,
            start.ToString("HH:mm"),
            "Test",
            "calendar",
            null,
            [],
            0,
            0,
            0.1,
            false,
            null,
            start,
            end);
    }
}
