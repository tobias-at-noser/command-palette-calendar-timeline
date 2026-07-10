using System.Collections.ObjectModel;
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
    private string statusText = string.Empty;

    public TimelineSnapbarViewModel(ISnapbarSnapshotClient snapshotClient)
    {
        this.snapshotClient = snapshotClient;
    }

    public ObservableCollection<TimelineBlockViewModel> Blocks { get; } = [];

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
                    block.Lane,
                    block.StartRatio,
                    Math.Max(0, block.EndRatio - block.StartRatio),
                    block.IsRunning,
                    block.Appointment.TeamsUrl))
                .ToArray();

            Blocks.Clear();
            foreach (var block in projectedBlocks)
            {
                Blocks.Add(block);
            }

            StatusText = snapshot.StatusMessage ?? string.Empty;
        }
        catch
        {
            Blocks.Clear();
            StatusText = UnavailableStatusText;
        }
    }

    public static void OpenTeamsUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
