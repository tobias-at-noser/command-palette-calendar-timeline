using System.Text.Json;
using CalendarTimeline.Core;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class CalendarSnapshotJsonTests
{
    [Fact]
    public void SerializeUsesCamelCaseIpcFieldNames()
    {
        var snapshot = CreateSnapshot("Outlook-Kalender nicht verfügbar");

        var json = CalendarSnapshotJson.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("generatedAt", out _));
        Assert.True(document.RootElement.TryGetProperty("windowStart", out _));
        Assert.True(document.RootElement.TryGetProperty("windowEnd", out _));
        Assert.True(document.RootElement.TryGetProperty("appointments", out var appointments));
        Assert.True(document.RootElement.TryGetProperty("statusMessage", out var status));
        Assert.True(appointments[0].TryGetProperty("teamsUrl", out _));
        Assert.Equal("Outlook-Kalender nicht verfügbar", status.GetString());
    }

    [Fact]
    public void DeserializeRoundTripsSnapshot()
    {
        var snapshot = CreateSnapshot(null);
        var json = CalendarSnapshotJson.Serialize(snapshot);

        var roundTripped = CalendarSnapshotJson.Deserialize(json);

        Assert.Equal(snapshot.GeneratedAt, roundTripped.GeneratedAt);
        Assert.Equal(snapshot.WindowStart, roundTripped.WindowStart);
        Assert.Equal(snapshot.WindowEnd, roundTripped.WindowEnd);
        Assert.Single(roundTripped.Appointments);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/fake", roundTripped.Appointments[0].TeamsUrl);
    }

    private static CalendarSnapshot CreateSnapshot(string? statusMessage)
    {
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        return new CalendarSnapshot(
            now,
            now.AddMinutes(-30),
            now.AddHours(4),
            [new Appointment("1", "Termin", "Raum", now, now.AddMinutes(30), false, false, "https://teams.microsoft.com/l/meetup-join/fake")],
            statusMessage);
    }
}
