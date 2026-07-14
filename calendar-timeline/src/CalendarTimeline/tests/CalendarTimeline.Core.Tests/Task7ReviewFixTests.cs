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
        Assert.Contains("Text = block.StartTime,", source);
        Assert.Contains("Text = block.Title,", source);
        Assert.Contains("TextTrimming = TextTrimming.CharacterEllipsis", source);
        Assert.Contains("CreateBubbleFill(colors.LightFill, colors.DarkFill)", source);
    }

    [Fact]
    public void SnapbarSourceSeparatesManualHeightFromAutomaticLaneHeight()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var sizeChanged = source[
            source.IndexOf("private void OnSizeChanged", StringComparison.Ordinal)..
            source.IndexOf("private void OnLocationChanged", StringComparison.Ordinal)];
        var updateWindowHeight = source[
            source.IndexOf("private void UpdateWindowHeight", StringComparison.Ordinal)..
            source.IndexOf("private void RestoreWindowSettings", StringComparison.Ordinal)];

        Assert.Contains("private double manualWindowHeight;", source);
        Assert.Contains("private bool isUpdatingWindowHeight;", source);
        Assert.Contains("if (!isUpdatingWindowHeight)", sizeChanged);
        Assert.Contains("manualWindowHeight = Math.Max(MinHeight, Height);", sizeChanged);
        Assert.Contains("TimelineSnapbarLayout.GetWindowHeight(manualWindowHeight, requiredHeight)", source);
        Assert.True(
            IndexOf(updateWindowHeight, "isUpdatingWindowHeight = true;")
            < IndexOf(updateWindowHeight, "MinHeight = minimumWindowHeight;"));
        Assert.True(
            IndexOf(updateWindowHeight, "MinHeight = minimumWindowHeight;")
            < IndexOf(updateWindowHeight, "Height = targetHeight;"));
        Assert.True(
            IndexOf(updateWindowHeight, "Height = targetHeight;")
            < IndexOf(updateWindowHeight, "isUpdatingWindowHeight = false;"));
    }

    [Fact]
    public void SnapbarSourceUsesRestoredHeightAsTheManualFloorBeforeAutomaticLayoutUpdates()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var restoreWindowSettings = source[
            source.IndexOf("private void RestoreWindowSettings", StringComparison.Ordinal)..
            source.IndexOf("private void PersistWindowSettings", StringComparison.Ordinal)];
        var constructor = source[
            source.IndexOf("public MainWindow(TimelineSnapbarViewModel viewModel)", StringComparison.Ordinal)..
            source.IndexOf("private async void OnLoaded", StringComparison.Ordinal)];

        Assert.True(
            IndexOf(restoreWindowSettings, "Height = settings.Height;")
            < IndexOf(restoreWindowSettings, "manualWindowHeight = Math.Max(MinHeight, Height);"));
        Assert.True(
            IndexOf(constructor, "RestoreWindowSettings();")
            < IndexOf(constructor, "SizeChanged += OnSizeChanged;"));
    }

    [Fact]
    public void SnapbarSourceRendersPolishedBlocksAndNowIndicators()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));

        Assert.Contains("<Grid.OpacityMask>", xaml);
        Assert.Contains("x:Key=\"TimelineBlockButtonStyle\"", xaml);
        Assert.Contains("CornerRadius=\"5\"", xaml);
        Assert.Contains("HorizontalAlignment=\"{TemplateBinding HorizontalContentAlignment}\"", xaml);
        Assert.Contains("VerticalAlignment=\"{TemplateBinding VerticalContentAlignment}\"", xaml);
        Assert.Contains("x:Name=\"NowTimeTextBlock\"", xaml);
        Assert.Contains("x:Name=\"CountdownTextBlock\"", xaml);
        Assert.Contains("Style = (Style)FindResource(\"TimelineBlockButtonStyle\")", source);
        Assert.Contains("Text = block.StartTime,", source);
        Assert.Contains("TimelineTimeDisplay.GetCountdown(now, viewModel.Blocks)", source);
        Assert.Contains("TimelineTimeDisplay.GetDateTooltip(now)", source);
    }

    [Fact]
    public void SnapbarSourceKeepsTimelineFadesFixedAndSeparatesTimeIndicators()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var blocksViewportStart = xaml.IndexOf("x:Name=\"BlocksViewport\"", StringComparison.Ordinal);
        var nowTime = xaml[xaml.IndexOf("<Border x:Name=\"NowTimeIndicator\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"NowTimeIndicator\"", StringComparison.Ordinal), StringComparison.Ordinal)];
        var countdown = xaml[xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal), StringComparison.Ordinal)];

        Assert.True(blocksViewportStart >= 0, "Could not find BlocksViewport.");
        var blocksViewport = xaml[blocksViewportStart..];
        Assert.Contains("<Grid.OpacityMask>", blocksViewport);
        Assert.Contains("Offset=\"0\"", blocksViewport);
        Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeInEndRatio}\"", blocksViewport);
        Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeOutStartRatio}\"", blocksViewport);
        Assert.Contains("Offset=\"1\"", blocksViewport);
        Assert.Contains("HorizontalAlignment=\"Right\"", nowTime);
        Assert.Contains("VerticalAlignment=\"Bottom\"", nowTime);
        Assert.Contains("HorizontalAlignment=\"Left\"", countdown);
        Assert.Contains("VerticalAlignment=\"Top\"", countdown);
        Assert.Contains("timelineHeight - nowLineBounds.Bottom", source);
        Assert.Contains("timelineWidth - (timelineWidth * TimelineSnapbarLayout.NowRatio) + 4", source);
        Assert.Contains("CountdownIndicator.Margin", source);
    }

    [Fact]
    public void SnapbarCountdownIsTopAlignedOffsetAndHintsAtTheNextAppointment()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var countdownStart = xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal);
        var countdown = xaml[countdownStart..xaml.IndexOf("</Border>", countdownStart, StringComparison.Ordinal)];

        Assert.Contains("VerticalAlignment=\"Top\"", countdown);
        Assert.Contains("x:Name=\"CountdownTranslation\"", countdown);
        Assert.Contains("<EventTrigger RoutedEvent=\"FrameworkElement.Loaded\">", countdown);
        Assert.Contains("Storyboard.TargetName=\"CountdownTranslation\"", countdown);
        Assert.Contains("Storyboard.TargetProperty=\"X\"", countdown);
        Assert.Contains("To=\"3\"", countdown);
        Assert.Contains("Duration=\"0:0:1.2\"", countdown);
        Assert.Contains("AutoReverse=\"True\"", countdown);
        Assert.Contains("RepeatBehavior=\"Forever\"", countdown);
        Assert.Contains("timelineWidth * TimelineSnapbarLayout.NowRatio + 8", source);
        Assert.Contains("nowLineBounds.Top", source);
    }

    [Fact]
    public void SnapbarSourceUsesAClippedMaskedViewportForUnboundedBlocks()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var viewportStart = xaml.IndexOf("<Grid x:Name=\"BlocksViewport\"", StringComparison.Ordinal);
        var canvasStart = xaml.IndexOf("<Canvas x:Name=\"BlocksCanvas\"", StringComparison.Ordinal);
        var nowLine = xaml[xaml.IndexOf("<Border x:Name=\"NowLine\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"NowLine\"", StringComparison.Ordinal), StringComparison.Ordinal)];
        var nowTime = xaml[xaml.IndexOf("<Border x:Name=\"NowTimeIndicator\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"NowTimeIndicator\"", StringComparison.Ordinal), StringComparison.Ordinal)];
        var countdown = xaml[xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal)..xaml.IndexOf("</Border>", xaml.IndexOf("<Border x:Name=\"CountdownIndicator\"", StringComparison.Ordinal), StringComparison.Ordinal)];

        Assert.True(viewportStart >= 0, "Could not find BlocksViewport.");
        Assert.True(canvasStart > viewportStart, "BlocksCanvas must be a child of BlocksViewport.");
        var viewport = xaml[viewportStart..xaml.IndexOf("</Grid>", canvasStart, StringComparison.Ordinal)];
        var blocksCanvas = xaml[canvasStart..xaml.IndexOf("/>", canvasStart, StringComparison.Ordinal)];
        Assert.Contains("Panel.ZIndex=\"2\"", viewport);
        Assert.Contains("ClipToBounds=\"True\"", viewport);
        Assert.Contains("<Grid.OpacityMask>", viewport);
        Assert.Contains("x:Name=\"TimelineFadeMask\"", viewport);
        Assert.Contains("StartPoint=\"0,0\"", viewport);
        Assert.Contains("MappingMode=\"Absolute\"", viewport);
        Assert.Contains("Offset=\"0\"", viewport);
        Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeInEndRatio}\"", viewport);
        Assert.Contains("Offset=\"{x:Static snapbar:TimelineSnapbarLayout.FadeOutStartRatio}\"", viewport);
        Assert.Contains("Offset=\"1\"", viewport);
        Assert.DoesNotContain("MappingMode=\"RelativeToBoundingBox\"", viewport);
        Assert.DoesNotContain("Canvas.OpacityMask", blocksCanvas);
        Assert.Contains("Panel.ZIndex=\"3\"", nowLine);
        Assert.Contains("Panel.ZIndex=\"4\"", nowTime);
        Assert.Contains("Panel.ZIndex=\"4\"", countdown);
    }

    [Fact]
    public void SnapbarSourceUsesTimelineGridDimensionsForBlockGeometryAndAbsoluteMasking()
    {
        var xaml = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml"));
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var viewportStart = xaml.IndexOf("<Grid x:Name=\"BlocksViewport\"", StringComparison.Ordinal);
        var canvasStart = xaml.IndexOf("<Canvas x:Name=\"BlocksCanvas\"", StringComparison.Ordinal);
        var viewport = xaml[viewportStart..xaml.IndexOf("</Grid>", canvasStart, StringComparison.Ordinal)];
        var updateLayout = source[
            source.IndexOf("private void UpdateLayoutMetrics", StringComparison.Ordinal)..
            source.IndexOf("private void UpdateWindowHeight", StringComparison.Ordinal)];

        Assert.Contains("var timelineWidth = TimelineGrid.ActualWidth;", updateLayout);
        Assert.Contains("TimelineFadeMask.StartPoint = new Point(0, 0);", updateLayout);
        Assert.Contains("TimelineFadeMask.EndPoint = new Point(timelineWidth, 0);", updateLayout);
        Assert.Contains("BlocksViewport.Width = timelineWidth;", updateLayout);
        Assert.Contains("BlocksViewport.Height = timelineHeight;", updateLayout);
        Assert.Contains("BlocksCanvas.Width = timelineWidth;", updateLayout);
        Assert.Contains("BlocksCanvas.Height = timelineHeight;", updateLayout);
        Assert.Contains("HorizontalAlignment=\"Left\"", viewport);
        Assert.Contains("VerticalAlignment=\"Top\"", viewport);
        Assert.Equal(2, CountOccurrences(xaml, "TimelineSnapbarLayout.FadeInEndRatio"));
        Assert.Equal(2, CountOccurrences(xaml, "TimelineSnapbarLayout.FadeOutStartRatio"));
    }

    [Fact]
    public void SnapbarSourceRestoresTopmostZOrderWithoutTakingFocusAfterDeactivation()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));
        var handlerIndex = IndexOf(source, "private void OnDeactivated");
        var handler = source[handlerIndex..source.IndexOf("private void OnClosed", handlerIndex, StringComparison.Ordinal)];

        Assert.Contains("Deactivated += OnDeactivated;", source);
        Assert.Contains("private const uint SwpNoActivate = 0x0010;", source);
        Assert.Contains("HwndTopmost", handler);
        Assert.Contains("SwpNoActivate", handler);
    }

    [Fact]
    public void SnapbarSourceRendersCompactMetadataAndStructuredTooltips()
    {
        var source = File.ReadAllText(ResolveWpfSourcePath("MainWindow.xaml.cs"));

        Assert.Contains("private static Grid CreateBubbleLabel", source);
        Assert.Contains("new RowDefinition { Height = new GridLength(12) }", source);
        Assert.Contains("new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }", source);
        Assert.Contains("FontSize = 11", source);
        Assert.Contains("FontSize = 9", source);
        Assert.Contains("TextTrimming = TextTrimming.CharacterEllipsis", source);
        Assert.Contains("TextWrapping = TextWrapping.NoWrap", source);
        Assert.Contains("TimelineBubbleLayout.ShouldShowDuration(width)", source);
        Assert.Equal(2, CountOccurrences(source, "TimelineBubbleLayout.ShouldShowDuration(width)"));
        Assert.Contains("new ToolTip", source);
        Assert.Contains("Text = block.Title,", source);
        Assert.Contains("Text = block.StartTime + \"–\" + block.End.ToString(\"HH:mm\") + \" · \" + block.Duration,", source);
        Assert.Contains("Text = block.TooltipContext,", source);
        Assert.Contains("VerticalAlignment = VerticalAlignment.Center", source);
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

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var startIndex = 0;

        while ((startIndex = source.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += value.Length;
        }

        return count;
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
