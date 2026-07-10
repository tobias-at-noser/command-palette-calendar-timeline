using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class DockAgendaProjectorTests
{
    [Fact]
    public void Project_PrioritizesRunningAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("running", "Daily", "Teams", now.AddMinutes(-10), now.AddMinutes(20), false, false, "https://teams.microsoft.com/l/meetup-join/running"),
                new Appointment("later", "Review", "Raum 2", now.AddHours(1), now.AddHours(2), false, false, null),
            ],
            null);

        var items = DockAgendaProjector.Project(snapshot, 3);

        Assert.Equal("Jetzt · Daily", items[0].Title);
        Assert.Equal("20 Min. verbleibend · Teams", items[0].Subtitle);
        Assert.Equal(0, items[0].Lane);
        Assert.True(items[0].IsRunning);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/running", items[0].TeamsUrl);
        Assert.Equal("running", items[0].AppointmentId);
    }

    [Fact]
    public void Project_AddsNextAppointmentAfterRunningAppointment()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 20, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("running", "Daily", "Teams", now.AddMinutes(-5), now.AddMinutes(10), false, false, "https://teams.microsoft.com/l/meetup-join/running"),
                new Appointment("next", "Planning", "Room", new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero), false, false, null),
            ],
            null);

        var items = DockAgendaProjector.Project(snapshot, 3);

        Assert.Equal(2, items.Count);
        Assert.Equal("Jetzt · Daily", items[0].Title);
        Assert.Equal("Als Nächstes · Planning", items[1].Title);
        Assert.Equal("09:30–10:00 · Room", items[1].Subtitle);
        Assert.Equal(0, items[1].Lane);
        Assert.False(items[1].IsRunning);
        Assert.Equal("next", items[1].AppointmentId);
    }

    [Fact]
    public void Project_PrefixesFirstUpcomingAppointmentWhenNothingIsRunning()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("next", "Planning", "Room", new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero), false, false, null),
                new Appointment("later", "Review", "Room 2", new DateTimeOffset(2026, 7, 9, 10, 15, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 9, 10, 45, 0, TimeSpan.Zero), false, false, null),
            ],
            null);

        var items = DockAgendaProjector.Project(snapshot, 3);

        Assert.Equal(2, items.Count);
        Assert.Equal("Als Nächstes · Planning", items[0].Title);
        Assert.Equal("09:30–10:00 · Room", items[0].Subtitle);
        Assert.Equal("Review", items[1].Title);
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
            now.AddHours(4),
            [new Appointment("private", "Board Review", "Raum 1", now, now.AddMinutes(30), isPrivate, isConfidential, null)],
            null);

        var items = DockAgendaProjector.Project(snapshot, 3);

        Assert.Single(items);
        Assert.DoesNotContain("Board Review", items[0].Title);
        Assert.Contains("Privater Termin", items[0].Title);
        Assert.DoesNotContain("Raum 1", items[0].Subtitle);
    }
}
