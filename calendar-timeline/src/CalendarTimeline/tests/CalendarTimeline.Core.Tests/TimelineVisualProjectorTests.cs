using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineVisualProjectorTests
{
    [Fact]
    public void Project_ComputesNormalizedStartAndEndWithinWindow()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero);
        var windowStart = new DateTimeOffset(2026, 7, 9, 9, 0, 0, TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(2026, 7, 9, 11, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            windowStart,
            windowEnd,
            [new Appointment("1", "Planning", "Room", new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero), false, false, null)],
            null);

        var blocks = TimelineVisualProjector.Project(snapshot);

        Assert.Single(blocks);
        Assert.Equal(0.25, blocks[0].StartRatio, 3);
        Assert.Equal(0.5, blocks[0].EndRatio, 3);
        Assert.True(blocks[0].IsRunning);
        Assert.Equal(0, blocks[0].Lane);
    }

    [Fact]
    public void Project_ClampsRatiosForAppointmentsOutsideWindow()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero);
        var windowStart = new DateTimeOffset(2026, 7, 9, 9, 0, 0, TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            windowStart,
            windowEnd,
            [new Appointment("1", "Workshop", "Room", new DateTimeOffset(2026, 7, 9, 8, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 9, 10, 30, 0, TimeSpan.Zero), false, false, null)],
            null);

        var blocks = TimelineVisualProjector.Project(snapshot);

        Assert.Single(blocks);
        Assert.Equal(0, blocks[0].StartRatio);
        Assert.Equal(1, blocks[0].EndRatio);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Project_HidesPrivateAndConfidentialTitles(bool isPrivate, bool isConfidential)
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(1),
            [new Appointment("private", "Board Review", "Raum 1", now, now.AddMinutes(30), isPrivate, isConfidential, null)],
            null);

        var blocks = TimelineVisualProjector.Project(snapshot);

        Assert.Single(blocks);
        Assert.DoesNotContain("Board Review", blocks[0].DisplayTitle);
        Assert.Equal("Privater Termin", blocks[0].DisplayTitle);
        Assert.DoesNotContain("Raum 1", blocks[0].DisplaySubtitle);
    }
}
