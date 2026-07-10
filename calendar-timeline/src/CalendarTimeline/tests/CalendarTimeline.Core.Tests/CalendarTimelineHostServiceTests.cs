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

    [Fact]
    public async Task RefreshSnapshotRequestClearsCachedSnapshotWhenLaterRefreshFails()
    {
        var snapshot = CreateSnapshot();
        var service = new CalendarTimelineHostService(
            new HostSnapshotCache(),
            new SucceedsThenFailsHostSnapshotSource(snapshot));

        var firstResponse = await service.HandleAsync(new RefreshSnapshotRequest(), TestContext.Current.CancellationToken);
        var failedRefreshResponse = await service.HandleAsync(new RefreshSnapshotRequest(), TestContext.Current.CancellationToken);
        var cachedResponse = await service.HandleAsync(new GetSnapshotRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(snapshot, Assert.IsType<SnapshotResponse>(firstResponse).Snapshot);
        Assert.Equal("Kalenderdaten nicht verfügbar", Assert.IsType<ErrorResponse>(failedRefreshResponse).Message);
        Assert.Equal("Kalenderdaten nicht verfügbar", Assert.IsType<ErrorResponse>(cachedResponse).Message);
    }

    [Fact]
    public async Task RefreshSnapshotRequestPropagatesCancellationAndPreservesCachedSnapshot()
    {
        var snapshot = CreateSnapshot();
        var cache = new HostSnapshotCache();
        cache.Update(snapshot, "ok");
        var service = new CalendarTimelineHostService(cache, new CancellingHostSnapshotSource());
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.HandleAsync(new RefreshSnapshotRequest(), cancellationSource.Token));
        var cachedResponse = await service.HandleAsync(new GetSnapshotRequest(), CancellationToken.None);

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(snapshot, Assert.IsType<SnapshotResponse>(cachedResponse).Snapshot);
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

    private sealed class SucceedsThenFailsHostSnapshotSource(CalendarSnapshot snapshot) : IHostSnapshotSource
    {
        private int calls;

        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            calls++;
            return calls == 1
                ? Task.FromResult(snapshot)
                : Task.FromException<CalendarSnapshot>(new InvalidOperationException());
        }
    }

    private sealed class CancellingHostSnapshotSource : IHostSnapshotSource
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromCanceled<CalendarSnapshot>(cancellationToken);
        }
    }
}
