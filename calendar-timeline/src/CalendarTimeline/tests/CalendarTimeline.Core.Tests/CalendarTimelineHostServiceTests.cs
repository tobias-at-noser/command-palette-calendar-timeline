using CalendarTimeline.Core;
using CalendarTimeline.Host;
using CalendarTimeline.Ipc;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarTimelineHostServiceTests
{
    [Fact]
    public async Task RefreshSnapshotRequestCachesWorkerSnapshot()
    {
        var snapshot = CreateSnapshot();
        var service = new CalendarTimelineHostService(new HostSnapshotCache(), new StubHostSnapshotSource(snapshot));

        var response = await service.HandleAsync(new RefreshSnapshotRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(snapshot, Assert.IsType<SnapshotResponse>(response).Snapshot);
    }

    [Fact]
    public async Task RefreshSnapshotRequestReturnsUnavailableWhenSourceFails()
    {
        var service = new CalendarTimelineHostService(new HostSnapshotCache(), new FailingHostSnapshotSource());

        var response = await service.HandleAsync(new RefreshSnapshotRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("Kalenderdaten nicht verfügbar", Assert.IsType<ErrorResponse>(response).Message);
    }

    private static CalendarSnapshot CreateSnapshot()
    {
        var now = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
        return new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
    }

    private sealed class StubHostSnapshotSource(CalendarSnapshot snapshot) : IHostSnapshotSource
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FailingHostSnapshotSource : IHostSnapshotSource
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromException<CalendarSnapshot>(new InvalidOperationException());
        }
    }
}
