using System.Text.Json;
using CalendarTimeline.Core;

namespace CalendarTimeline.Ipc;

public static class CalendarTimelinePipeJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string SerializeRequest(CalendarTimelineRequest request)
    {
        return request switch
        {
            PingRequest pingRequest => JsonSerializer.Serialize(pingRequest, Options),
            GetSnapshotRequest getSnapshotRequest => JsonSerializer.Serialize(getSnapshotRequest, Options),
            RefreshSnapshotRequest refreshSnapshotRequest => JsonSerializer.Serialize(refreshSnapshotRequest, Options),
            _ => throw new JsonException($"Unsupported calendar timeline request type '{request.GetType().Name}'.")
        };
    }

    public static CalendarTimelineRequest DeserializeRequest(string json)
    {
        using var document = JsonDocument.Parse(json);
        var type = GetType(document.RootElement);

        return type switch
        {
            "ping" => Deserialize<PingRequest>(json),
            "getSnapshot" => Deserialize<GetSnapshotRequest>(json),
            "refreshSnapshot" => Deserialize<RefreshSnapshotRequest>(json),
            _ => throw new JsonException($"Unsupported calendar timeline request type '{type}'.")
        };
    }

    public static string SerializeResponse(CalendarTimelineResponse response)
    {
        return response switch
        {
            StatusResponse statusResponse => JsonSerializer.Serialize(statusResponse, Options),
            SnapshotResponse snapshotResponse => JsonSerializer.Serialize(snapshotResponse, Options),
            ErrorResponse errorResponse => JsonSerializer.Serialize(errorResponse, Options),
            _ => throw new JsonException($"Unsupported calendar timeline response type '{response.GetType().Name}'.")
        };
    }

    public static CalendarTimelineResponse DeserializeResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var type = GetType(document.RootElement);

        return type switch
        {
            "status" => Deserialize<StatusResponse>(json),
            "snapshot" => Deserialize<SnapshotResponse>(json),
            "error" => Deserialize<ErrorResponse>(json),
            _ => throw new JsonException($"Unsupported calendar timeline response type '{type}'.")
        };
    }

    private static string GetType(JsonElement element)
    {
        if (!element.TryGetProperty("type", out var typeProperty) || string.IsNullOrWhiteSpace(typeProperty.GetString()))
        {
            throw new JsonException("Calendar timeline pipe JSON is missing a valid 'type'.");
        }

        return typeProperty.GetString()!;
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new JsonException($"Calendar timeline pipe JSON was empty for {typeof(T).Name}.");
    }
}
