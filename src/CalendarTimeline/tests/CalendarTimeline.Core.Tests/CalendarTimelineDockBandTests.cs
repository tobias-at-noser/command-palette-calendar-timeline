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
}
