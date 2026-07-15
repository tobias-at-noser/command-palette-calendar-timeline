using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CalendarTimeline.Core;
using CalendarTimeline.Ipc;

namespace CalendarTimeline.Snapbar;

public interface ISnapbarSnapshotClient
{
    Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class PipeSnapbarSnapshotClient : ISnapbarSnapshotClient
{
    private readonly CalendarTimelinePipeClient pipeClient;

    public PipeSnapbarSnapshotClient()
        : this(new CalendarTimelinePipeClient())
    {
    }

    public PipeSnapbarSnapshotClient(CalendarTimelinePipeClient pipeClient)
    {
        this.pipeClient = pipeClient;
    }

    public async Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        return await pipeClient.SendAsync(new RefreshSnapshotRequest(), cancellationToken) switch
        {
            SnapshotResponse response => response.Snapshot,
            ErrorResponse error => throw new InvalidOperationException(error.Message),
            StatusResponse status => throw new InvalidOperationException(status.Status),
            _ => throw new InvalidOperationException("Unexpected snapbar response."),
        };
    }
}

public sealed class TimelineSnapbarViewModel : INotifyPropertyChanged
{
    public const string UnavailableStatusText = "Kalenderdaten nicht verfügbar";

    private readonly ISnapbarSnapshotClient snapshotClient;
    private AllDayTagViewModel? allDayTag;
    private IReadOnlyList<TimelineBlockViewModel> blocks = [];
    private string statusText = string.Empty;

    public TimelineSnapbarViewModel(ISnapbarSnapshotClient snapshotClient)
    {
        this.snapshotClient = snapshotClient;
    }

    public IReadOnlyList<TimelineBlockViewModel> Blocks => blocks;

    public AllDayTagViewModel? AllDayTag => allDayTag;

    public string StatusText
    {
        get => statusText;
        private set
        {
            if (statusText == value)
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await snapshotClient.LoadSnapshotAsync(cancellationToken);
            var projectedBlocks = TimelineVisualProjector.Project(snapshot)
                .OrderBy(block => block.Lane)
                .ThenBy(block => block.StartRatio)
                .Select(block => new TimelineBlockViewModel(
                    block.DisplayTitle,
                    block.DisplaySubtitle,
                    block.DisplayStartTime,
                    block.DisplayDuration,
                    block.DisplaySubtitle,
                    block.TooltipContext,
                    block.Appointment.CalendarId,
                    block.CalendarColor,
                    block.CategoryColors,
                    block.Lane,
                    block.StartRatio,
                    Math.Max(0, block.EndRatio - block.StartRatio),
                    block.IsRunning,
                    block.Appointment.TeamsUrl,
                    block.Appointment.Start,
                    block.Appointment.End))
                .ToArray();
            var tags = TimelineVisualProjector.ProjectAllDayTags(snapshot);
            var firstTag = tags.FirstOrDefault();
            var projectedAllDayTag = firstTag is null
                ? null
                : new AllDayTagViewModel(
                    firstTag.DisplayTitle,
                    tags.Count - 1,
                    tags.Select(tag => tag.DisplayTitle).ToArray(),
                    firstTag.Appointment.CalendarId,
                    firstTag.CalendarColor,
                    firstTag.CategoryColors,
                    firstTag.Appointment.Start,
                    firstTag.Appointment.End);

            ApplyRefreshResult(projectedBlocks, projectedAllDayTag, snapshot.StatusMessage ?? string.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException exception) when (exception.Message.StartsWith($"{UnavailableStatusText}:", StringComparison.Ordinal))
        {
            ApplyRefreshResult([], null, exception.Message);
        }
        catch
        {
            ApplyRefreshResult([], null, UnavailableStatusText);
        }
    }

    private void ApplyRefreshResult(
        IReadOnlyList<TimelineBlockViewModel> nextBlocks,
        AllDayTagViewModel? nextAllDayTag,
        string nextStatusText)
    {
        blocks = nextBlocks;
        allDayTag = nextAllDayTag;
        StatusText = nextStatusText;
        OnPropertyChanged(nameof(Blocks));
        OnPropertyChanged(nameof(AllDayTag));
    }

    public static void OpenTeamsUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public void ReportDisplayUnavailable()
    {
        StatusText = UnavailableStatusText;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
