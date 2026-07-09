using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class TimelineLayoutTests
{
    [Fact]
    public void ArrangeUsesSameLaneForNonOverlappingAppointments()
    {
        var start = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var first = CreateAppointment("1", start, start.AddMinutes(30));
        var second = CreateAppointment("2", start.AddMinutes(30), start.AddMinutes(60));

        var blocks = TimelineLayout.Arrange([first, second]);

        Assert.Equal([0, 0], blocks.Select(block => block.Lane));
    }

    [Fact]
    public void ArrangeUsesSeparateLanesForOverlappingAppointments()
    {
        var start = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        var first = CreateAppointment("1", start, start.AddMinutes(60));
        var second = CreateAppointment("2", start.AddMinutes(15), start.AddMinutes(45));
        var third = CreateAppointment("3", start.AddMinutes(60), start.AddMinutes(90));

        var blocks = TimelineLayout.Arrange([first, second, third]);

        Assert.Equal([0, 1, 0], blocks.Select(block => block.Lane));
    }

    private static Appointment CreateAppointment(string id, DateTimeOffset start, DateTimeOffset end)
    {
        return new Appointment(id, $"Appointment {id}", "Room", start, end, false, false, null);
    }
}
