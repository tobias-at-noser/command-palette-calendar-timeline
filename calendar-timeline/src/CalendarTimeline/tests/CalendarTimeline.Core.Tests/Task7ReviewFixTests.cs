using System.Reflection;
using CalendarTimeline.Host;
using Xunit;

namespace CalendarTimeline.Core.Tests;

public sealed class Task7ReviewFixTests
{
    [Fact]
    public async Task ShowAsyncReturnsMissingStatusWhenCalendarTimelineWpfExeIsUnavailable()
    {
        var controller = new SnapbarProcessController();

        var status = await controller.ShowAsync(CancellationToken.None);

        Assert.Equal("Timeline-Anwendung nicht gefunden.", status);
    }

    [Fact]
    public void TrayApplicationContextSourceListsCommandsInRequiredOrder()
    {
        var sourcePath = ResolveTrayApplicationContextPath();
        var source = File.ReadAllText(sourcePath);

        Assert.True(IndexOf(source, "menu.Items.Add(\"Timeline anzeigen\"")
            < IndexOf(source, "menu.Items.Add(\"Timeline verbergen\""));
        Assert.True(IndexOf(source, "menu.Items.Add(\"Timeline verbergen\"")
            < IndexOf(source, "menu.Items.Add(autostartMenuItem)"));
        Assert.True(IndexOf(source, "menu.Items.Add(autostartMenuItem)")
            < IndexOf(source, "menu.Items.Add(\"Jetzt aktualisieren\""));
        Assert.True(IndexOf(source, "menu.Items.Add(\"Jetzt aktualisieren\"")
            < IndexOf(source, "menu.Items.Add(\"Beenden\""));
        Assert.Contains("autostartMenuItem.Text = autostartManager.IsEnabled() ? \"Autostart deaktivieren\" : \"Autostart aktivieren\";", source);
    }

    [Fact]
    public void TrayApplicationContextSourceCancelsHostBeforeExitThread()
    {
        var sourcePath = ResolveTrayApplicationContextPath();
        var source = File.ReadAllText(sourcePath);

        Assert.Contains("private readonly Action requestShutdown;", source);
        Assert.Contains("public TrayApplicationContext(CalendarTimelineHostService service, CancellationToken cancellationToken, Action requestShutdown)", source);
        Assert.Contains("this.requestShutdown = requestShutdown;", source);
        Assert.Contains("menu.Items.Add(\"Beenden\"", source);
        Assert.True(IndexOf(source, "requestShutdown();") < IndexOf(source, "ExitThread();"));
    }

    [Fact]
    public void HostStartupAndTrayRefreshHandleExpectedShutdownCancellation()
    {
        var programSource = File.ReadAllText(ResolveHostSourcePath("Program.cs"));
        var traySource = File.ReadAllText(ResolveHostSourcePath("TrayApplicationContext.cs"));

        Assert.Contains("catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)", programSource);
        Assert.Contains("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", traySource);
    }

    [Fact]
    public void ResolveTrayApplicationContextPathFindsSourceWithoutRuntimeIdentifierDirectory()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var testAssemblyDirectory = Path.Combine(rootDirectory, "tests", "CalendarTimeline.Core.Tests", "bin", "Debug", "net10.0");
        var expectedPath = Path.Combine(rootDirectory, "src", "CalendarTimeline.Host", "TrayApplicationContext.cs");

        try
        {
            Directory.CreateDirectory(testAssemblyDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.WriteAllText(expectedPath, string.Empty);

            Assert.Equal(expectedPath, ResolveTrayApplicationContextPath(testAssemblyDirectory));
        }
        finally
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private static int IndexOf(string source, string value)
    {
        var index = source.IndexOf(value, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Could not find '{value}' in TrayApplicationContext.cs.");
        return index;
    }

    private static string ResolveTrayApplicationContextPath(string? testAssemblyDirectory = null)
    {
        return ResolveHostSourcePath("TrayApplicationContext.cs", testAssemblyDirectory);
    }

    private static string ResolveHostSourcePath(string fileName, string? testAssemblyDirectory = null)
    {
        var directory = new DirectoryInfo(testAssemblyDirectory ?? AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "CalendarTimeline.Host",
                fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate TrayApplicationContext.cs.");
    }
}
