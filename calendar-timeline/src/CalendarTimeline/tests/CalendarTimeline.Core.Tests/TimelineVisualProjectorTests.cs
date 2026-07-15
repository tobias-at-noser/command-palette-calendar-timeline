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
    public void Project_PreservesRatiosForAppointmentsOutsideWindow()
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
        Assert.Equal(-0.5, blocks[0].StartRatio, 3);
        Assert.Equal(1.5, blocks[0].EndRatio, 3);
    }

    [Fact]
    public void Project_KeepsLocationInTooltipSubtitle()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Planning", "Room 42", now, now.AddMinutes(30), false, false, null)],
            null);

        var block = Assert.Single(TimelineVisualProjector.Project(snapshot));

        Assert.Equal("09:30–10:00 · Room 42", block.DisplaySubtitle);
    }

    [Fact]
    public void Project_AppendsCalendarAndCategoriesToTooltipAndProjectsRawColors()
    {
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment(
                "1", "Planning", "Room 42", now, now.AddMinutes(30), false, false, null,
                "work", "Arbeit", "#3B82B6",
                [new CalendarCategory("Fokus", "#D83B01"), new CalendarCategory("Kunde", "#8764B8")])],
            null);

        var block = Assert.Single(TimelineVisualProjector.Project(snapshot));

        Assert.Equal("10:00–10:30 · Room 42 · Arbeit · Fokus, Kunde", block.DisplaySubtitle);
        Assert.Equal("30 Min.", block.DisplayDuration);
        Assert.Equal("Room 42 · Arbeit · Fokus, Kunde", block.TooltipContext);
        Assert.Equal("#3B82B6", block.CalendarColor);
        Assert.Equal(["#D83B01", "#8764B8"], block.CategoryColors);
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
            [new Appointment(
                "private", "Board Review", "Raum 1", now, now.AddMinutes(30), isPrivate, isConfidential, null,
                "private-calendar", "Privat", "#3B82B6", [new CalendarCategory("Finance", "#FF0000")])],
            null);

        var blocks = TimelineVisualProjector.Project(snapshot);

        Assert.Single(blocks);
        Assert.DoesNotContain("Board Review", blocks[0].DisplayTitle);
        Assert.Equal("Privater Termin", blocks[0].DisplayTitle);
        Assert.DoesNotContain("Raum 1", blocks[0].DisplaySubtitle);
        Assert.DoesNotContain("Finance", blocks[0].DisplaySubtitle);
        Assert.Equal(["12:00–12:30", "Privat"], blocks[0].DisplaySubtitle.Split(" · "));
        Assert.Empty(blocks[0].CategoryColors);
    }

    [Fact]
    public void ProjectAllDayTags_OrdersSanitizesAndPreservesColorMetadata()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var earlier = now.Date;
        var snapshot = new CalendarSnapshot(
            now,
            now.AddHours(-2),
            now.AddHours(2),
            [
                new Appointment("timed", "Timed", "Room", now, now.AddMinutes(30), false, false, null),
                new Appointment("b", "Board Review", "Room", earlier.AddDays(1), earlier.AddDays(2), true, false, null,
                    "private", "Privat", "#3B82B6", [new CalendarCategory("Finance", "#FF0000")], true),
                new Appointment("a", "Earlier", "Room", earlier, earlier.AddDays(1), false, false, null,
                    "work", "Arbeit", "#D83B01", [new CalendarCategory("Focus", "#8764B8")], true),
            ],
            null);

        var tags = TimelineVisualProjector.ProjectAllDayTags(snapshot);

        Assert.Equal(["a", "b"], tags.Select(tag => tag.Appointment.Id));
        Assert.Equal("Privater Termin", tags[1].DisplayTitle);
        Assert.Equal("#D83B01", tags[0].CalendarColor);
        Assert.Equal(["#8764B8"], tags[0].CategoryColors);
        Assert.Empty(tags[1].CategoryColors);
    }
}
