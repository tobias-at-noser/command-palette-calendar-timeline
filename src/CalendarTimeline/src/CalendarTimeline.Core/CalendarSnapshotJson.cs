using System.Text.Json;

namespace CalendarTimeline.Core;

public static class CalendarSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(CalendarSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static CalendarSnapshot Deserialize(string json)
    {
        return JsonSerializer.Deserialize<CalendarSnapshot>(json, Options)
            ?? throw new JsonException("Calendar snapshot JSON was empty.");
    }
}
