using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelineCommandsProviderTests
{
    [Fact]
    public void TryApplyWorkerSnapshotUpdatesDockBandFromJson()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var provider = new CalendarTimelineCommandsProvider();

        var applied = provider.TryApplyWorkerSnapshot(CalendarSnapshotJson.Serialize(snapshot));

        Assert.True(applied);
        Assert.Equal(snapshot.GeneratedAt, provider.DockBand.Snapshot?.GeneratedAt);
        Assert.Single(provider.DockBand.Blocks);
        Assert.Equal(string.Empty, provider.DockBand.StatusMessage);
    }

    [Fact]
    public void TryApplyWorkerSnapshotKeepsLastSnapshotWhenJsonIsInvalid()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var provider = new CalendarTimelineCommandsProvider();
        provider.Update(snapshot);

        var applied = provider.TryApplyWorkerSnapshot("not-json");

        Assert.False(applied);
        Assert.Same(snapshot, provider.DockBand.Snapshot);
        Assert.Equal("Outlook-Kalender nicht verfügbar", provider.DockBand.StatusMessage);
        Assert.Single(provider.DockBand.Blocks);
    }
}
