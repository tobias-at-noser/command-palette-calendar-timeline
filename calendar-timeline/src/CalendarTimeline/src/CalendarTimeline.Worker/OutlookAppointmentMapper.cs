using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public static class OutlookAppointmentMapper
{
    public static CalendarSnapshot CreateSnapshot(
        DateTimeOffset now,
        IEnumerable<OutlookAppointmentData> appointments,
        string? statusMessage = null)
    {
        var window = CalendarWindow.Create(now);
        var mappedAppointments = appointments
            .Where(appointment => appointment.Start < window.End && appointment.End > window.Start)
            .OrderBy(appointment => appointment.Start)
            .ThenBy(appointment => appointment.End)
            .Select(appointment => AppointmentSanitizer.Sanitize(new Appointment(
                appointment.Id,
                appointment.Title,
                appointment.Location,
                appointment.Start,
                appointment.End,
                appointment.IsPrivate,
                appointment.IsConfidential,
                TeamsLinkDetector.TryFind(appointment.Body),
                appointment.CalendarId,
                appointment.CalendarName,
                appointment.CalendarColor,
                appointment.Categories ?? [],
                appointment.IsAllDayEvent)))
            .ToArray();

        return new CalendarSnapshot(
            now,
            window.Start,
            window.End,
            mappedAppointments,
            statusMessage);
    }
}
