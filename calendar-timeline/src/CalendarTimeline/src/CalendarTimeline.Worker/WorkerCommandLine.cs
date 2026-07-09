using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public static class WorkerCommandLine
{
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken,
        ICalendarSnapshotSource? outlookSource = null)
    {
        if (args.Contains("--fake-once", StringComparer.OrdinalIgnoreCase))
        {
            await WriteSnapshotAsync(new FakeCalendarSnapshotSource(), output, cancellationToken);
            return 0;
        }

        if (args.Contains("--outlook-once", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await WriteSnapshotAsync(outlookSource ?? new OutlookCalendarSnapshotSource(), output, cancellationToken);
                return 0;
            }
            catch
            {
                error.WriteLine("Outlook-Kalender nicht verfügbar");
                return 2;
            }
        }

        error.WriteLine("Use --fake-once or --outlook-once to emit one calendar snapshot.");
        return 1;
    }

    private static async Task WriteSnapshotAsync(ICalendarSnapshotSource source, TextWriter output, CancellationToken cancellationToken)
    {
        var snapshot = await source.LoadSnapshotAsync(DateTimeOffset.Now, cancellationToken);
        output.WriteLine(CalendarSnapshotJson.Serialize(snapshot));
    }
}
