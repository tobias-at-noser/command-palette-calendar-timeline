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

    private static int IndexOf(string source, string value)
    {
        var index = source.IndexOf(value, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Could not find '{value}' in TrayApplicationContext.cs.");
        return index;
    }

    private static string ResolveTrayApplicationContextPath()
    {
        var testAssemblyDirectory = AppContext.BaseDirectory;
        var configurationDirectory = Directory.GetParent(testAssemblyDirectory)?.Parent;
        var tfmDirectory = configurationDirectory?.Parent;
        var testsProjectDirectory = tfmDirectory?.Parent?.Parent?.Parent;
        Assert.NotNull(testsProjectDirectory);
        return Path.Combine(testsProjectDirectory!.FullName, "..", "src", "CalendarTimeline.Host", "TrayApplicationContext.cs");
    }
}
