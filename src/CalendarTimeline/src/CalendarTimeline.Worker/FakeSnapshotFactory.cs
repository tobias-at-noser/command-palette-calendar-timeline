using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public static class FakeSnapshotFactory
{
    public static CalendarSnapshot Create(DateTimeOffset now)
    {
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
                "fake-private",
                "Vorstandsgespräch",
                "Raum 42",
                now.AddMinutes(45),
                now.AddMinutes(90),
                true,
                false,
                null)),
            AppointmentSanitizer.Sanitize(new Appointment(
                "fake-overlap",
                "Projekt Sync",
                "Focus Room",
                now.AddMinutes(60),
                now.AddMinutes(120),
                false,
                false,
                null))
        };

        return new CalendarSnapshot(now, window.Start, window.End, appointments, null);
    }
}
