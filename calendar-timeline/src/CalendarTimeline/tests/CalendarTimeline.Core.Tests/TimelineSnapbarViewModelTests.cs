using CalendarTimeline.Core;
using CalendarTimeline.Ipc;
using CalendarTimeline.Snapbar;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineSnapbarViewModelTests
{
    [Fact]
    public async Task RefreshAsync_MapsRunningAppointmentToHighlightedBlock()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 30, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(2),
            [
                new Appointment("running", "Daily", "Teams", now.AddMinutes(-10), now.AddMinutes(20), false, false, "https://teams.microsoft.com/l/meetup-join/running"),
            ],
            null);
        var viewModel = new TimelineSnapbarViewModel(new StubSnapbarSnapshotClient(snapshot));

        await viewModel.RefreshAsync(CancellationToken.None);

        var block = Assert.Single(viewModel.Blocks);
        Assert.True(block.IsRunning);
        Assert.Equal("Daily", block.Title);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/running", block.TeamsUrl);
    }

    [Fact]
    public async Task RefreshAsync_SetsUnavailableStatusWhenSnapshotClientFails()
    {
        var viewModel = new TimelineSnapbarViewModel(new FailingSnapbarSnapshotClient());

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal("Kalenderdaten nicht verfügbar", viewModel.StatusText);
        Assert.Empty(viewModel.Blocks);
    }

    [Fact]
    public async Task RefreshAsync_SortsBlocksByLaneThenStartRatio()
    {
        var now = new DateTimeOffset(2026, 7, 9, 9, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now,
            now.AddHours(2),
            [
                new Appointment("lane-one", "Lane One", "Room 1", now.AddMinutes(10), now.AddMinutes(70), false, false, null),
                new Appointment("lane-zero-early", "Lane Zero Early", "Room 0", now, now.AddMinutes(30), false, false, null),
                new Appointment("lane-zero-late", "Lane Zero Late", "Room 0", now.AddMinutes(30), now.AddMinutes(60), false, false, null),
            ],
            null);
        var viewModel = new TimelineSnapbarViewModel(new StubSnapbarSnapshotClient(snapshot));

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal(["Lane Zero Early", "Lane Zero Late", "Lane One"], viewModel.Blocks.Select(block => block.Title).ToArray());
        Assert.Equal([0, 0, 1], viewModel.Blocks.Select(block => block.Lane).ToArray());
    }

    [Fact]
    public async Task RefreshAsync_ProvidesTimeAndLocationForTheBubbleTooltip()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var viewModel = new TimelineSnapbarViewModel(new StubSnapbarSnapshotClient(
            new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4),
                [new Appointment("1", "Planning", "Room 42", now, now.AddMinutes(30), false, false, null)], null)));

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Planning", Assert.Single(viewModel.Blocks).Title);
        Assert.Equal("10:00–10:30 · Room 42", viewModel.Blocks.Single().Subtitle);
    }

    [Fact]
    public void TimelineBlockViewModel_ExposesOnlySharedProjectionState()
    {
        var propertyNames = typeof(TimelineBlockViewModel)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal([
            "HasTeamsUrl",
            "IsRunning",
            "Lane",
            "StartRatio",
            "Subtitle",
            "TeamsUrl",
            "Title",
            "WidthRatio",
        ], propertyNames);
    }

    [Fact]
    public async Task PipeSnapshotClientRequestsRefreshBeforeLoadingTheSnapbarSnapshot()
    {
        var pipeName = $"calendar-timeline-test-{Guid.NewGuid():N}";
        var snapshot = CreateSnapshot();
        var server = new CalendarTimelinePipeServer(pipeName);
        using var cancellationSource = new CancellationTokenSource();
        CalendarTimelineRequest? receivedRequest = null;
        var serverTask = server.RunAsync(
            (request, _) =>
            {
                receivedRequest = request;
                return Task.FromResult<CalendarTimelineResponse>(new SnapshotResponse(snapshot));
            },
            cancellationSource.Token);

        try
        {
            var client = new PipeSnapbarSnapshotClient(new CalendarTimelinePipeClient(pipeName));

            var loadedSnapshot = await client.LoadSnapshotAsync(TestContext.Current.CancellationToken);

            Assert.Equal(snapshot.GeneratedAt, loadedSnapshot.GeneratedAt);
            Assert.Equal(snapshot.WindowStart, loadedSnapshot.WindowStart);
            Assert.Equal(snapshot.WindowEnd, loadedSnapshot.WindowEnd);
            Assert.IsType<RefreshSnapshotRequest>(receivedRequest);
        }
        finally
        {
            cancellationSource.Cancel();
            await serverTask;
        }
    }

    private static CalendarSnapshot CreateSnapshot()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        return new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4), [], null);
    }

    private sealed class StubSnapbarSnapshotClient(CalendarSnapshot snapshot) : ISnapbarSnapshotClient
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FailingSnapbarSnapshotClient : ISnapbarSnapshotClient
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
