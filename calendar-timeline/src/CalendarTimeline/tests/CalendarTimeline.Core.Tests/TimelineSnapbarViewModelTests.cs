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
    public async Task RefreshAsync_ShowsSafeHostUnavailableStatus()
    {
        const string status = "Kalenderdaten nicht verfügbar: Outlook-Aktualisierung hat das Zeitlimit überschritten.";
        var viewModel = new TimelineSnapbarViewModel(new SafeStatusFailingSnapbarSnapshotClient(status));

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal(status, viewModel.StatusText);
        Assert.Empty(viewModel.Blocks);
    }

    [Fact]
    public async Task RefreshAsync_PreservesExistingStateAndRethrowsWhenCallerCancels()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var initialSnapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Planning", "Room 42", now, now.AddMinutes(30), false, false, null)],
            "Aktualisiert");
        var viewModel = new TimelineSnapbarViewModel(new CancellingAfterInitialSnapshotClient(initialSnapshot));
        await viewModel.RefreshAsync(CancellationToken.None);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => viewModel.RefreshAsync(cancellationSource.Token));

        Assert.Equal("Aktualisiert", viewModel.StatusText);
        Assert.Equal("Planning", Assert.Single(viewModel.Blocks).Title);
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
    public async Task RefreshAsync_PublishesCompleteBlockSetAfterAnEmptySnapshot()
    {
        var now = new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero);
        var emptySnapshot = new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4), [], null);
        var populatedSnapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("new", "Neu", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);
        var viewModel = new TimelineSnapbarViewModel(
            new SequentialSnapbarSnapshotClient(emptySnapshot, populatedSnapshot));
        var blockPublications = 0;
        viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(TimelineSnapbarViewModel.Blocks))
            {
                blockPublications++;
            }
        };

        await viewModel.RefreshAsync(CancellationToken.None);
        blockPublications = 0;

        await viewModel.RefreshAsync(CancellationToken.None);

        var block = Assert.Single(viewModel.Blocks);
        Assert.Equal("Neu", block.Title);
        Assert.Equal(0, block.Lane);
        Assert.Equal(1, blockPublications);
    }

    [Fact]
    public async Task RefreshAsync_PublishesOverlappingBlocksTogetherAfterAnEmptySnapshot()
    {
        var now = new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero);
        var emptySnapshot = new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4), [], null);
        var populatedSnapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("early", "Früh", "Raum", now, now.AddMinutes(30), false, false, null),
                new Appointment("overlap", "Überlappend", "Raum", now.AddMinutes(15), now.AddMinutes(45), false, false, null),
            ],
            null);
        var viewModel = new TimelineSnapbarViewModel(
            new SequentialSnapbarSnapshotClient(emptySnapshot, populatedSnapshot));
        var blockPublications = 0;
        viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(TimelineSnapbarViewModel.Blocks))
            {
                blockPublications++;
            }
        };

        await viewModel.RefreshAsync(CancellationToken.None);
        blockPublications = 0;

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal(["Früh", "Überlappend"], viewModel.Blocks.Select(block => block.Title).ToArray());
        Assert.Equal([0, 1], viewModel.Blocks.Select(block => block.Lane).ToArray());
        Assert.Equal(1, blockPublications);
    }

    [Fact]
    public async Task RefreshAsync_ProvidesTimeAndLocationForTheBubbleTooltip()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var viewModel = new TimelineSnapbarViewModel(new StubSnapbarSnapshotClient(
            new CalendarSnapshot(now, now.AddMinutes(-30), now.AddHours(4),
                [new Appointment(
                    "1", "Planning", "Room 42", now, now.AddMinutes(30), false, false, null,
                    "work", "Arbeit", "#3B82B6",
                    [new CalendarCategory("Fokus", "#D83B01"), new CalendarCategory("Kunde", "#8764B8")])], null)));

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        var block = Assert.Single(viewModel.Blocks);
        Assert.Equal("Planning", block.Title);
        Assert.Equal("10:00", block.StartTime);
        Assert.Equal("10:00–10:30 · Room 42 · Arbeit · Fokus, Kunde", block.Tooltip);
        Assert.Equal("30 Min.", block.Duration);
        Assert.Equal("Room 42 · Arbeit · Fokus, Kunde", block.TooltipContext);
        Assert.Equal("work", block.CalendarIdentity);
        Assert.Equal("#3B82B6", block.CalendarColor);
        Assert.Equal(["#D83B01", "#8764B8"], block.CategoryColors);
    }

    [Fact]
    public async Task RefreshAsync_MapsFirstAllDayTagWithAggregateContext()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var earlier = new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero);
        var snapshot = new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [
                new Appointment("timed", "Timed", "Room", now, now.AddMinutes(30), false, false, null),
                new Appointment("later", "Later", "Room", earlier.AddDays(1), earlier.AddDays(2), false, false, null,
                    "work", "Arbeit", "#3B82B6", [new CalendarCategory("Focus", "#D83B01")], true),
                new Appointment("earlier", "Earlier", "Room", earlier, earlier.AddDays(1), false, false, null,
                    "private", "Privat", "#8764B8", [new CalendarCategory("Personal", "#8764B8")], true),
            ],
            null);
        var viewModel = new TimelineSnapbarViewModel(new StubSnapbarSnapshotClient(snapshot));

        await viewModel.RefreshAsync(CancellationToken.None);

        var tag = viewModel.AllDayTag!;
        Assert.Equal("Earlier", tag.Title);
        Assert.Equal(1, tag.AdditionalCount);
        Assert.Equal(["Earlier", "Later"], tag.TooltipTitles);
        Assert.Equal("private", tag.CalendarIdentity);
        Assert.Equal("#8764B8", tag.CalendarColor);
        Assert.Equal(["#8764B8"], tag.CategoryColors);
        Assert.Equal(earlier, tag.Start);
        Assert.Equal(earlier.AddDays(1), tag.End);
        Assert.Single(viewModel.Blocks);
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
            "CalendarColor",
            "CalendarIdentity",
            "CategoryColors",
            "Duration",
            "End",
            "HasTeamsUrl",
            "IsRunning",
            "Lane",
            "Start",
            "StartRatio",
            "StartTime",
            "Subtitle",
            "TeamsUrl",
            "Title",
            "Tooltip",
            "TooltipContext",
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

    private sealed class SequentialSnapbarSnapshotClient(params CalendarSnapshot[] snapshots) : ISnapbarSnapshotClient
    {
        private int index;

        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(snapshots[index++]);
        }
    }

    private sealed class FailingSnapbarSnapshotClient : ISnapbarSnapshotClient
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class SafeStatusFailingSnapbarSnapshotClient(string status) : ISnapbarSnapshotClient
    {
        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(status);
        }
    }

    private sealed class CancellingAfterInitialSnapshotClient(CalendarSnapshot initialSnapshot) : ISnapbarSnapshotClient
    {
        private bool hasLoadedInitialSnapshot;

        public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
        {
            if (!hasLoadedInitialSnapshot)
            {
                hasLoadedInitialSnapshot = true;
                return Task.FromResult(initialSnapshot);
            }

            return Task.FromCanceled<CalendarSnapshot>(cancellationToken);
        }
    }
}
