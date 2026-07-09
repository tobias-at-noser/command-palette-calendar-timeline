namespace CalendarTimeline.Core;

public static class AppointmentSanitizer
{
    public static Appointment Sanitize(Appointment appointment)
    {
        if (!appointment.IsPrivate && !appointment.IsConfidential)
        {
            return appointment;
        }

        return appointment with
        {
            Title = "Privater Termin",
            Location = string.Empty,
            TeamsUrl = null
        };
    }
}
