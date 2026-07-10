namespace CalendarTimeline.Host;

public sealed class AutostartManager
{
    private const string AppName = "CalendarTimeline.Host";

    public bool IsEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var command = GetLaunchCommand();
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var shortcutPath = GetShortcutPath();
        if (File.Exists(shortcutPath))
        {
            return true;
        }

        return ReadRunKey() is string existingCommand
            && string.Equals(existingCommand, command, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (enabled)
        {
            WriteRunKey(GetLaunchCommand());
            return;
        }

        DeleteShortcutIfPresent();
        DeleteRunKey();
    }

    private static string GetLaunchCommand()
    {
        var executablePath = Environment.ProcessPath;
        return string.IsNullOrWhiteSpace(executablePath) ? string.Empty : $"\"{executablePath}\"";
    }

    private static string GetShortcutPath()
    {
        var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(startupPath, $"{AppName}.lnk");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? ReadRunKey()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: false);
            return key?.GetValue(AppName) as string;
        }
        catch
        {
            return null;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void WriteRunKey(string command)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            key.SetValue(AppName, command);
        }
        catch
        {
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void DeleteRunKey()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch
        {
        }
    }

    private static void DeleteShortcutIfPresent()
    {
        try
        {
            var shortcutPath = GetShortcutPath();
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
        catch
        {
        }
    }
}
