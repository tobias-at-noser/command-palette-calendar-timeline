using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelineCommandsProviderTests
{
    [Fact]
    public async Task RefreshFromHostUpdatesDockBandFromHostSnapshot()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var hostClient = new StubHostSnapshotClient(snapshot);
        var provider = new CalendarTimelineCommandsProvider(hostClient);

        var refreshed = await provider.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(refreshed);
        Assert.Equal(1, hostClient.Calls);
        Assert.Same(snapshot, provider.DockBand.Snapshot);
        Assert.Equal(string.Empty, provider.DockBand.StatusMessage);
    }

    [Fact]
    public async Task RefreshFromHostKeepsLastSnapshotWhenHostFails()
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var provider = new CalendarTimelineCommandsProvider(new FailingHostSnapshotClient());
        provider.Update(snapshot);

        var refreshed = await provider.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(refreshed);
        Assert.Same(snapshot, provider.DockBand.Snapshot);
        Assert.Equal("Kalenderdaten nicht verfügbar", provider.DockBand.StatusMessage);
        Assert.Single(provider.DockBand.Rows);
    }

    [Fact]
    public void DefaultConstructorUsesPipeHostSnapshotClient()
    {
        var provider = new CalendarTimelineCommandsProvider();

        Assert.IsType<PipeHostSnapshotClient>(provider.HostSnapshotClient);
    }
}
