using CalendarTimeline.Core;
using CalendarTimeline.Host;
using CalendarTimeline.Ipc;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class HostSnapshotCacheTests
{
    [Fact]
    public void GetSnapshotResponseReturnsErrorWhenSnapshotMissing()
    {
        var cache = new HostSnapshotCache();

        var response = cache.GetSnapshotResponse();

        var error = Assert.IsType<ErrorResponse>(response);
        Assert.Equal("Kalenderdaten nicht verfügbar", error.Message);
    }

    [Fact]
    public void GetSnapshotResponseReturnsSnapshotAfterUpdate()
    {
        var cache = new HostSnapshotCache();
        var snapshot = CreateSnapshot();

        cache.Update(snapshot, "ok");
        var response = cache.GetSnapshotResponse();

        var snapshotResponse = Assert.IsType<SnapshotResponse>(response);
        Assert.Equal(snapshot, snapshotResponse.Snapshot);
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
}
