using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineTimeDisplayTests
{
    [Fact]
    public void GetCurrentTime_FormatsTheTimeDeterministically()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 7, 0, TimeSpan.Zero);

        Assert.Equal("09:07", TimelineTimeDisplay.GetCurrentTime(now));
    }

    [Fact]
    public void GetCountdown_SelectsTheNextFutureAppointmentWhileAnotherIsRunning()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);
        var running = CreateBlock(now.AddMinutes(-20), now.AddMinutes(30));
        var next = CreateBlock(now.AddMinutes(82), now.AddMinutes(112));

        var countdown = TimelineTimeDisplay.GetCountdown(now, [running, next]);

        Assert.Equal("01:20", countdown!.Text);
        Assert.Same(next, countdown.Target);
    }

    [Fact]
    public void GetCountdown_HidesTheCountdownWithoutAFutureAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(now, [CreateBlock(now.AddMinutes(-10), now.AddMinutes(10))]);

        Assert.Null(result);
    }

    [Fact]
    public void GetCountdown_HidesTheCountdownAtTheFiveMinuteBoundary()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(now, [CreateBlock(now.AddMinutes(5), now.AddMinutes(35))]);

        Assert.Null(result);
    }

    [Fact]
    public void GetCountdown_HidesTheCountdownForAnInvalidFutureAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(now, [CreateBlock(now.AddMinutes(30), now.AddMinutes(30))]);

        Assert.Null(result);
    }

    [Fact]
    public void GetCountdown_IgnoresAnInvalidFutureAppointmentBeforeTheNextValidAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.GetCountdown(
            now,
            [
                CreateBlock(now.AddMinutes(30), now.AddMinutes(20)),
                CreateBlock(now.AddMinutes(82), now.AddMinutes(112)),
            ]);

        Assert.Equal("01:20", result!.Text);
    }

    [Fact]
    public void GetCountdown_SelectsTheFutureAppointmentAfterAJustStartedAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);
        var justStarted = CreateBlock(now, now.AddMinutes(30));
        var next = CreateBlock(now.AddMinutes(25), now.AddMinutes(55));

        var countdown = TimelineTimeDisplay.GetCountdown(now, [justStarted, next]);

        Assert.Equal("00:25", countdown!.Text);
        Assert.Same(next, countdown.Target);
    }

    [Fact]
    public void IsHighlighted_CoversTheFiveMinuteLeadInUntilTheBlockEnds()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);
        var block = CreateBlock(now.AddMinutes(5), now.AddMinutes(35));

        Assert.True(TimelineTimeDisplay.IsHighlighted(now, block));
        Assert.True(TimelineTimeDisplay.IsHighlighted(now.AddMinutes(5), block));
        Assert.False(TimelineTimeDisplay.IsHighlighted(now.AddMinutes(35), block));
        Assert.True(TimelineTimeDisplay.IsHighlighted(now, CreateBlock(now.AddMinutes(-2), now.AddMinutes(20))));
    }

    [Fact]
    public void IsHighlighted_ReturnsFalseForAnInvalidBlock()
    {
        var now = new DateTimeOffset(2026, 7, 13, 9, 0, 0, TimeSpan.Zero);

        var result = TimelineTimeDisplay.IsHighlighted(now, CreateBlock(now.AddMinutes(5), now.AddMinutes(5)));

        Assert.False(result);
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
