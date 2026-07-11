using System.Text.Json;

namespace CalendarTimeline.Snapbar;

public sealed class SnapbarWindowSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public SnapbarWindowSettingsStore(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CalendarTimeline",
            "snapbar-window.json");
    }

    public string SettingsPath { get; }

    public SnapbarWindowSettings? Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return null;
            }

            var settings = JsonSerializer.Deserialize<SnapbarWindowSettings>(File.ReadAllText(SettingsPath), SerializerOptions);
            return settings is { } value && value.IsFiniteAndPositive() ? value : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(SnapbarWindowSettings settings)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
