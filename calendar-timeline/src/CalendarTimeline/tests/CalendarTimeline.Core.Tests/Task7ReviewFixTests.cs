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

        Assert.Contains("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", programSource);
        Assert.Contains("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", traySource);
    }

    [Fact]
    public void WindowsHostStartsPipeServerBeforeSchedulingInitialRefresh()
    {
        var programSource = File.ReadAllText(ResolveHostSourcePath("Program.cs"));

        Assert.True(IndexOf(programSource, "var serverTask = server.RunAsync")
            < IndexOf(programSource, "_ = RefreshInitialSnapshotAsync"));
        Assert.True(IndexOf(programSource, "_ = RefreshInitialSnapshotAsync")
            < IndexOf(programSource, "System.Windows.Forms.Application.Run(context);"));
    }

    [Fact]
    public void ConsoleCancelKeyPressRequestsTrayExitAfterCancellingHost()
    {
        var programSource = File.ReadAllText(ResolveHostSourcePath("Program.cs"));
        var traySource = File.ReadAllText(ResolveHostSourcePath("TrayApplicationContext.cs"));

        Assert.Contains("cancellationSource.Cancel();", programSource);
        Assert.Contains("cancellationToken.Register(context.ExitThreadSafely);", programSource);
        Assert.Contains("internal void ExitThreadSafely()", traySource);
        Assert.Contains("shutdownDispatcher.BeginInvoke((Action)ExitThread);", traySource);
    }

    [Fact]
    public void TrayLoopExitAndServerFailureCancelTheSharedHostToken()
    {
        var programSource = File.ReadAllText(ResolveHostSourcePath("Program.cs"));
        var applicationRunIndex = IndexOf(programSource, "System.Windows.Forms.Application.Run(context);");

        Assert.Contains("serverTask.ContinueWith", programSource);
        Assert.Contains("_ => cancellationSource.Cancel()", programSource);
        Assert.True(applicationRunIndex
            < IndexOf(programSource, "cancellationSource.Cancel();", applicationRunIndex));
        Assert.True(IndexOf(programSource, "cancellationSource.Cancel();", applicationRunIndex)
            < IndexOf(programSource, "await serverTask;", applicationRunIndex));
    }

    [Fact]
    public async Task HostShutdownDoesNotWaitForAnUnresponsivePipeServer()
    {
        var method = typeof(Program).GetMethod(
            "AwaitServerShutdownAsync",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(Task), typeof(CancellationToken)],
            modifiers: null);
        Assert.NotNull(method);

        var serverTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var shutdownTask = Assert.IsAssignableFrom<Task>(method.Invoke(null, [serverTask.Task, cancellationSource.Token]));
        await shutdownTask.WaitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void GenericWindowsFallbackAlsoStopsWaitingForThePipeServerOnShutdown()
    {
        var programSource = File.ReadAllText(ResolveHostSourcePath("Program.cs"));

        Assert.Contains(
            "#else\n        var serverTask = server.RunAsync(service.HandleAsync, cancellationToken);\n        await AwaitServerShutdownAsync(serverTask, cancellationToken);\n#endif",
            programSource);
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

    [Fact]
    public void SnapbarSourceRefreshesAtOneMinuteIntervalsWithoutOverlappingRequests()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));

        Assert.Contains("new DispatcherTimer", source);
        Assert.Contains("TimeSpan.FromMinutes(1)", source);
        Assert.Contains("if (refreshInProgress)", source);
        Assert.Contains("refreshInProgress = true;", source);
        Assert.Contains("refreshInProgress = false;", source);
        Assert.Contains("refreshTimer.Stop();", source);
        Assert.Contains("refreshTimer.Tick -= OnRefreshTimerTick;", source);
    }

    [Fact]
    public void SnapbarLayoutFailureMakesTheUnavailableStatusVisible()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var catchIndex = source.IndexOf("catch\n        {\n            BlocksCanvas.Children.Clear();", StringComparison.Ordinal);

        Assert.True(catchIndex >= 0, "Could not find the snapbar layout failure handler.");
        var failureHandler = source[catchIndex..source.IndexOf("finally", catchIndex, StringComparison.Ordinal)];
        Assert.Contains("viewModel.ReportDisplayUnavailable();", failureHandler);
        Assert.Contains("UpdateWindowHeight();", failureHandler);
    }

    [Fact]
    public void SnapbarSourcePreservesNowLineAndBubbleTextStructuralInvariants()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var nowLine = xaml[xaml.IndexOf("<Border x:Name=\"NowLine\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"NowLine\"", StringComparison.Ordinal), StringComparison.Ordinal)];

        Assert.Contains("Panel.ZIndex=\"3\"", nowLine);
        Assert.Contains("IsHitTestVisible=\"False\"", nowLine);
        Assert.Contains("Text = block.StartTime + \" · \"", source);
        Assert.Contains("Text = block.Title,", source);
        Assert.Contains("TextTrimming = TextTrimming.CharacterEllipsis", source);
        Assert.Contains("CreateBubbleFill(colors.LightFill, colors.DarkFill)", source);
    }

    [Fact]
    public void SnapbarSourceSeparatesManualHeightFromAutomaticLaneHeight()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));

        Assert.Contains("private double manualWindowHeight;", source);
        Assert.Contains("private bool isUpdatingWindowHeight;", source);
        Assert.Contains("if (!isUpdatingWindowHeight)", source);
        Assert.Contains("manualWindowHeight = Math.Max(MinHeight, Height);", source);
        Assert.Contains("TimelineSnapbarLayout.GetWindowHeight(manualWindowHeight, requiredHeight)", source);
    }

    [Fact]
    public void SnapbarSourceRendersPolishedBlocksAndNowIndicators()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));

        Assert.Contains("<Canvas.OpacityMask>", xaml);
        Assert.Contains("x:Key=\"TimelineBlockButtonStyle\"", xaml);
        Assert.Contains("CornerRadius=\"5\"", xaml);
        Assert.Contains("x:Name=\"NowTimeTextBlock\"", xaml);
        Assert.Contains("x:Name=\"CountdownTextBlock\"", xaml);
        Assert.Contains("Style = (Style)FindResource(\"TimelineBlockButtonStyle\")", source);
        Assert.Contains("Text = block.StartTime + \" · \"", source);
        Assert.Contains("TimelineTimeDisplay.GetCountdown(now, viewModel.Blocks)", source);
        Assert.Contains("TimelineTimeDisplay.GetDateTooltip(now)", source);
    }

    private static int IndexOf(string source, string value)
    {
        return IndexOf(source, value, 0);
    }

    private static int IndexOf(string source, string value, int startIndex)
    {
        var index = source.IndexOf(value, startIndex, StringComparison.Ordinal);
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

    private static string ResolveWpfSourcePath(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "CalendarTimeline.Wpf", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }
}
