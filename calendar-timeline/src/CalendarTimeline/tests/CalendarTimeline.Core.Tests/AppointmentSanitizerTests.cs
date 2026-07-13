using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class AppointmentSanitizerTests
{
    [Fact]
    public void SanitizeLeavesPublicAppointmentUnchanged()
    {
        var appointment = CreateAppointment(isPrivate: false, isConfidential: false);

        var sanitized = AppointmentSanitizer.Sanitize(appointment);

        Assert.Equal(appointment, sanitized);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void SanitizeMasksPrivateOrConfidentialAppointments(bool isPrivate, bool isConfidential)
    {
        var appointment = CreateAppointment(isPrivate, isConfidential);

        var sanitized = AppointmentSanitizer.Sanitize(appointment);

        Assert.Equal("Privater Termin", sanitized.Title);
        Assert.Equal(string.Empty, sanitized.Location);
        Assert.Null(sanitized.TeamsUrl);
        Assert.Equal(appointment.Start, sanitized.Start);
        Assert.Equal(appointment.End, sanitized.End);
    }

    [Fact]
    public void SanitizeClearsCategoriesButRetainsCalendarMetadataForProtectedAppointments()
    {
        var start = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var appointment = new Appointment(
            "private", "Budget", "Room", start, start.AddMinutes(30), true, false, null,
            "calendar-id", "Team", "#4B79A1", [new CalendarCategory("Finance", "#FF0000")]);

        var sanitized = AppointmentSanitizer.Sanitize(appointment);

        Assert.Empty(sanitized.Categories);
        Assert.Equal("calendar-id", sanitized.CalendarId);
        Assert.Equal("Team", sanitized.CalendarName);
        Assert.Equal("#4B79A1", sanitized.CalendarColor);
    }

    private static Appointment CreateAppointment(bool isPrivate, bool isConfidential)
    {
        var start = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        return new Appointment("1", "Strategy", "Room 1", start, start.AddMinutes(30), isPrivate, isConfidential, "https://teams.microsoft.com/l/meetup-join/example");
    }
}
