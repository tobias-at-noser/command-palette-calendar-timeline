using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelineDockBandTests
{
    [Fact]
    public async Task RefreshAsyncUsesHostSnapshotClientAndProjectsAgendaRows()
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
        var hostClient = new StubHostSnapshotClient(snapshot);
        var dockBand = new CalendarTimelineDockBand(hostClient);

        var refreshed = await dockBand.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(refreshed);
        Assert.Equal(1, hostClient.Calls);
        Assert.Same(snapshot, dockBand.Snapshot);
        Assert.Equal(string.Empty, dockBand.StatusMessage);
        Assert.Equal(3, dockBand.VisibleItemCount);
        Assert.Collection(
            dockBand.Rows,
            row =>
            {
                Assert.Equal("Jetzt · Daily", row.Title);
                Assert.Equal("20 Min. verbleibend · Teams", row.Subtitle);
                Assert.True(row.IsRunning);
                Assert.Equal("https://teams.microsoft.com/l/meetup-join/running", row.TeamsUrl);
            },
            row =>
            {
                Assert.Equal("Als Nächstes · Projekt Sync", row.Title);
                Assert.Equal("12:10–12:50 · Raum 1", row.Subtitle);
                Assert.False(row.IsRunning);
            },
            row =>
            {
                Assert.Equal("Review", row.Title);
                Assert.Equal("13:00–14:00 · Raum 2", row.Subtitle);
                Assert.False(row.IsRunning);
            });
    }

    [Fact]
    public async Task RefreshAsyncRendersUnavailableWhenHostClientFails()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var dockBand = new CalendarTimelineDockBand(new FailingHostSnapshotClient());
        dockBand.Update(snapshot);

        var refreshed = await dockBand.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(refreshed);
        Assert.Same(snapshot, dockBand.Snapshot);
        Assert.Equal("Kalenderdaten nicht verfügbar", dockBand.StatusMessage);
        Assert.Single(dockBand.Rows);
        Assert.Equal("Kalenderdaten nicht verfügbar", dockBand.Rows[0].Title);
        Assert.Equal(1, dockBand.VisibleItemCount);
    }

    [Fact]
    public void UpdateLimitsRowsToThreeProjectedAgendaItems()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("1", "A", "Raum 1", now.AddMinutes(-10), now.AddMinutes(10), false, false, null),
                new Appointment("2", "B", "Raum 2", now.AddMinutes(15), now.AddMinutes(30), false, false, null),
                new Appointment("3", "C", "Raum 3", now.AddMinutes(35), now.AddMinutes(50), false, false, null),
                new Appointment("4", "D", "Raum 4", now.AddMinutes(55), now.AddMinutes(70), false, false, null),
            ],
            null);
        var dockBand = new CalendarTimelineDockBand(new StubHostSnapshotClient(snapshot));

        dockBand.Update(snapshot);

        Assert.Equal(3, dockBand.VisibleItemCount);
        Assert.Collection(
            dockBand.Rows,
            row => Assert.Equal("Jetzt · A", row.Title),
            row => Assert.Equal("Als Nächstes · B", row.Title),
            row => Assert.Equal("C", row.Title));
    }
}
