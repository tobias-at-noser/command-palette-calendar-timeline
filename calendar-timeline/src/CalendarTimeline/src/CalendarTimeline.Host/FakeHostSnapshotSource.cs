using CalendarTimeline.Core;

namespace CalendarTimeline.Host;

public sealed class FakeHostSnapshotSource : IHostSnapshotSource
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var window = CalendarWindow.Create(now);
        var appointments = new[]
        {
            AppointmentSanitizer.Sanitize(new Appointment(
                "fake-running",
                "Daily Standup",
                "Teams",
                now.AddMinutes(-10),
                now.AddMinutes(20),
                false,
                false,
                TeamsLinkDetector.TryFind("https://teams.microsoft.com/l/meetup-join/fake"))),
            AppointmentSanitizer.Sanitize(new Appointment(
                "fake-next",
                "Projekt Sync",
                "Focus Room",
                now.AddMinutes(45),
                now.AddMinutes(90),
                false,
                false,
                null))
        };

        return Task.FromResult(new CalendarSnapshot(now, window.Start, window.End, appointments, null));
    }
}
