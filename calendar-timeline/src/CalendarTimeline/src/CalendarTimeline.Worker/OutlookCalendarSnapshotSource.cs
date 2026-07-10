using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public sealed class OutlookCalendarSnapshotSource : ICalendarSnapshotSource
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if WINDOWS
        return Task.FromResult(OutlookAppointmentMapper.CreateSnapshot(now, LoadAppointments(now)));
#else
        throw new PlatformNotSupportedException("Outlook COM calendar loading requires Windows and Outlook Desktop.");
#endif
    }

#if WINDOWS
    private static IReadOnlyList<OutlookAppointmentData> LoadAppointments(DateTimeOffset now)
    {
        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook Desktop is not installed or not registered.");
        dynamic outlook = Activator.CreateInstance(outlookType)
            ?? throw new InvalidOperationException("Outlook Desktop could not be initialized.");
        dynamic outlookNamespace = outlook.GetNamespace("MAPI");
        dynamic calendarFolder = outlookNamespace.GetDefaultFolder(9);
        dynamic items = calendarFolder.Items;
        items.IncludeRecurrences = true;
        items.Sort("[Start]");

        var window = CalendarWindow.Create(now);
        var filter = $"[End] > '{window.Start.LocalDateTime:g}' AND [Start] < '{window.End.LocalDateTime:g}'";
        dynamic restrictedItems = items.Restrict(filter);
        var appointments = new List<OutlookAppointmentData>();

        foreach (dynamic item in restrictedItems)
        {
            DateTime start = item.Start;
            DateTime end = item.End;
            var sensitivity = Convert.ToInt32(item.Sensitivity);
            appointments.Add(new OutlookAppointmentData(
                Convert.ToString(item.EntryID) ?? Guid.NewGuid().ToString("N"),
                Convert.ToString(item.Subject) ?? string.Empty,
                Convert.ToString(item.Location) ?? string.Empty,
                new DateTimeOffset(start),
                new DateTimeOffset(end),
                sensitivity == 2,
                sensitivity == 3,
                Convert.ToString(item.Body)));
        }

        return appointments;
    }
#endif
}
