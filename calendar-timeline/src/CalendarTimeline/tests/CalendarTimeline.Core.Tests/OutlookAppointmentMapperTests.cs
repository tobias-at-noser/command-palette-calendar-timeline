using CalendarTimeline.Worker;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class OutlookAppointmentMapperTests
{
    [Fact]
    public void CreateSnapshotFiltersSortsAndSanitizesOutlookAppointments()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var rawAppointments = new[]
        {
            new OutlookAppointmentData(
                "outside-before",
                "Outside Before",
                "Room 1",
                now.AddHours(-2),
                now.AddHours(-1),
                false,
                false,
                ""),
            new OutlookAppointmentData(
                "later",
                "Projekt Sync",
                "Room 2",
                now.AddHours(1),
                now.AddHours(2),
                false,
                false,
                "Join https://teams.microsoft.com/l/meetup-join/example"),
            new OutlookAppointmentData(
                "running-private",
                "Doctor",
                "Clinic",
                now.AddMinutes(-10),
                now.AddMinutes(20),
                true,
                false,
                "https://teams.microsoft.com/l/meetup-join/private"),
            new OutlookAppointmentData(
                "earlier",
                "Planning",
                "Room 3",
                now.AddMinutes(30),
                now.AddMinutes(45),
                false,
                false,
                null),
            new OutlookAppointmentData(
                "outside-after",
                "Outside After",
                "Room 4",
                now.AddHours(5),
                now.AddHours(6),
                false,
                false,
                ""),
        };

        var snapshot = OutlookAppointmentMapper.CreateSnapshot(now, rawAppointments);

        Assert.Equal(now, snapshot.GeneratedAt);
        Assert.Equal(now.AddMinutes(-30), snapshot.WindowStart);
        Assert.Equal(now.AddHours(4), snapshot.WindowEnd);
        Assert.Null(snapshot.StatusMessage);
        Assert.Equal(new[] { "running-private", "earlier", "later" }, snapshot.Appointments.Select(appointment => appointment.Id));
        Assert.Equal("Privater Termin", snapshot.Appointments[0].Title);
        Assert.Equal(string.Empty, snapshot.Appointments[0].Location);
        Assert.Null(snapshot.Appointments[0].TeamsUrl);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/example", snapshot.Appointments[2].TeamsUrl);
    }

    [Fact]
    public void CreateSnapshotKeepsAppointmentsThatOverlapWindowBoundaries()
    {
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var rawAppointments = new[]
        {
            new OutlookAppointmentData("starts-before", "Starts Before", "", now.AddHours(-1), now.AddMinutes(-20), false, false, null),
            new OutlookAppointmentData("ends-after", "Ends After", "", now.AddHours(3), now.AddHours(5), false, false, null),
            new OutlookAppointmentData("ends-at-window", "Ends At Window", "", now.AddHours(-1), now.AddMinutes(-30), false, false, null),
            new OutlookAppointmentData("starts-at-end", "Starts At End", "", now.AddHours(4), now.AddHours(5), false, false, null),
        };

        var snapshot = OutlookAppointmentMapper.CreateSnapshot(now, rawAppointments);

        Assert.Equal(new[] { "starts-before", "ends-after" }, snapshot.Appointments.Select(appointment => appointment.Id));
    }
}
