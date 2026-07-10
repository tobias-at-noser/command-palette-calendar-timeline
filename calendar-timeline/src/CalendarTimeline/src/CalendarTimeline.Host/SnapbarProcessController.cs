using System.Diagnostics;
using System.Reflection;

namespace CalendarTimeline.Host;

public sealed class SnapbarProcessController
{
    private Process? process;

    public async Task<string> ShowAsync(CancellationToken cancellationToken)
    {
        if (process is { HasExited: false })
        {
            return "Timeline läuft bereits.";
        }

        var executablePath = ResolveExecutablePath();
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return "Timeline-Anwendung nicht gefunden.";
        }

        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory
            });

            await Task.CompletedTask;
            return process is null ? "Timeline konnte nicht gestartet werden." : "Timeline wird angezeigt.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return $"Timeline konnte nicht gestartet werden: {ex.Message}";
        }
    }

    public async Task<string> HideAsync(CancellationToken cancellationToken)
    {
        if (process is null || process.HasExited)
        {
            return "Timeline ist nicht geöffnet.";
        }

        try
        {
            if (!process.CloseMainWindow())
            {
                process.Kill(true);
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            return "Timeline wurde verborgen.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            return $"Timeline konnte nicht verborgen werden: {ex.Message}";
        }
        finally
        {
            process.Dispose();
            process = null;
        }
    }

    private static string ResolveExecutablePath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        const string fileName = "CalendarTimeline.Wpf.exe";
        var directPath = Path.Combine(baseDirectory, fileName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "CalendarTimeline.Host";
        var configurationDirectory = Directory.GetParent(baseDirectory);
        var tfmDirectory = configurationDirectory?.Parent;
        var hostProjectDirectory = tfmDirectory?.Parent?.Parent?.Parent;
        var srcDirectory = hostProjectDirectory?.Parent;
        if (srcDirectory is null)
        {
            return directPath;
        }

        var wpfOutputPath = Path.Combine(srcDirectory.FullName, "CalendarTimeline.Wpf", "bin", tfmDirectory?.Name ?? string.Empty, fileName);
        return wpfOutputPath;
    }
}
