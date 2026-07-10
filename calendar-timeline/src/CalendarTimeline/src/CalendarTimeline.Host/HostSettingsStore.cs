using System.Text.Json;

namespace CalendarTimeline.Host;

public sealed class HostSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public HostSettingsStore(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? GetDefaultSettingsPath();
    }

    public string SettingsPath { get; }

    public async Task<HostSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsPath))
        {
            return HostSettings.Default;
        }

        await using var stream = File.OpenRead(SettingsPath);
        var settings = await JsonSerializer.DeserializeAsync<HostSettings>(stream, SerializerOptions, cancellationToken);
        return settings ?? HostSettings.Default;
    }

    public async Task SaveAsync(HostSettings settings, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
    }

    private static string GetDefaultSettingsPath()
    {
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppDataPath, "CalendarTimeline", "settings.json");
    }
}
