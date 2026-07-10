using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelineDockBandTests
{
    [Fact]
    public void ApplyWorkerErrorKeepsLastSnapshotAndShowsStatus()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var dockBand = new CalendarTimelineDockBand();
        dockBand.Update(snapshot);

        dockBand.ApplyWorkerError("Outlook-Kalender nicht verfügbar");

        Assert.Same(snapshot, dockBand.Snapshot);
        Assert.Equal("Outlook-Kalender nicht verfügbar", dockBand.StatusMessage);
        Assert.Single(dockBand.Blocks);
    }

    [Fact]
    public void UpdateClearsPreviousErrorStatusWhenSnapshotHasNoStatus()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4), [], null);
        var dockBand = new CalendarTimelineDockBand();
        dockBand.ApplyWorkerError("Outlook-Kalender nicht verfügbar");

        dockBand.Update(snapshot);

        Assert.Equal(string.Empty, dockBand.StatusMessage);
    }

    [Fact]
    public void UpdateBuildsTimelineRowsWithNowTeamsAndOverlapInformation()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("running", "Daily", "Teams", now.AddMinutes(-10), now.AddMinutes(20), false, false, "https://teams.microsoft.com/l/meetup-join/running"),
                new Appointment("overlap", "Projekt Sync", "Raum 1", now.AddMinutes(10), now.AddMinutes(50), false, false, null),
                new Appointment("later", "Review", "Raum 2", now.AddHours(1), now.AddHours(2), false, false, null),
            ],
            null);
        var dockBand = new CalendarTimelineDockBand();

        dockBand.Update(snapshot);

        Assert.Equal(3, dockBand.VisibleItemCount);
        Assert.Collection(
            dockBand.Rows,
            row =>
            {
                Assert.Equal("Jetzt · Daily", row.Title);
                Assert.Equal("20 Min. verbleibend · Teams", row.Subtitle);
                Assert.Equal(0, row.Lane);
                Assert.True(row.IsRunning);
                Assert.True(row.HasTeamsLink);
                Assert.Equal("https://teams.microsoft.com/l/meetup-join/running", row.TeamsUrl);
            },
            row =>
            {
                Assert.Equal("Projekt Sync", row.Title);
                Assert.Equal("12:10–12:50 · Raum 1", row.Subtitle);
                Assert.Equal(1, row.Lane);
                Assert.False(row.IsRunning);
                Assert.False(row.HasTeamsLink);
            },
            row =>
            {
                Assert.Equal("Review", row.Title);
                Assert.Equal("13:00–14:00 · Raum 2", row.Subtitle);
                Assert.Equal(0, row.Lane);
                Assert.False(row.IsRunning);
            });
    }

    [Fact]
    public void UpdateShowsCountdownWhenNoAppointmentIsRunning()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("next", "Fokus", "Büro", now.AddMinutes(45), now.AddMinutes(90), false, false, null)],
            null);
        var dockBand = new CalendarTimelineDockBand();

        dockBand.Update(snapshot);

        Assert.Equal("Nächster Termin in 45 Min.", dockBand.FreeTimeSummary);
    }
}
