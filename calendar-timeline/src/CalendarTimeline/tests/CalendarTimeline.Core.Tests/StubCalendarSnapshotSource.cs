using CalendarTimeline.Core;
using CalendarTimeline.Worker;

namespace CalendarTimeline.Core.Tests;

internal sealed class StubCalendarSnapshotSource(string appointmentId) : ICalendarSnapshotSource
{
    public int CallCount { get; private set; }

    public Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        CallCount++;
        var window = CalendarWindow.Create(now);
        var snapshot = new CalendarSnapshot(
            now,
            window.Start,
            window.End,
            [new Appointment(appointmentId, "Termin", "Raum", now, now.AddMinutes(30), false, false, null)],
            null);

        return Task.FromResult(snapshot);
    }
}
