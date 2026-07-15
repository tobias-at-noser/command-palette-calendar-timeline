using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using CalendarTimeline.Core;
using CalendarTimeline.Snapbar;

namespace CalendarTimeline.Wpf;

public partial class MainWindow : Window
{
    private const double GridVerticalMargin = 6;
    private const double StatusRowHeight = 16;
    private const int WmNcHitTest = 0x0084;
    private const int WmSysCommand = 0x0112;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcMouseLeave = 0x02A2;
    private const int GwlStyle = -16;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtTopLeft = 13;
    private const int HtTop = 12;
    private const int HtTopRight = 14;
    private const int HtRight = 11;
    private const int HtBottomRight = 17;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const uint TmeLeave = 0x00000002;
    private const uint TmeNonClient = 0x00000010;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private static readonly IntPtr HwndTopmost = new(-1);
    private readonly TimelineSnapbarViewModel viewModel;
    private readonly SnapbarWindowSettingsStore settingsStore = new();
    private readonly DispatcherTimer refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
    private bool refreshInProgress;
    private bool isUpdatingLayout;
    private bool isUpdatingWindowHeight;
    private double minimumWindowHeight;
    private double manualWindowHeight;
    private HwndSource? hwndSource;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventData
    {
        public uint Size;
        public uint Flags;
        public IntPtr WindowHandle;
        public uint HoverTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRect rectangle);

    [DllImport("user32.dll")]
    private static extern bool TrackMouseEvent(ref TrackMouseEventData eventData);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    public MainWindow()
        : this(new TimelineSnapbarViewModel(new PipeSnapbarSnapshotClient()))
    {
    }

    public MainWindow(TimelineSnapbarViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        Topmost = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        Width = SnapbarWindowSettings.DefaultWidth;
        Left = 0;
        Top = 0;
        manualWindowHeight = Height;
        UpdateWindowHeight();
        RestoreWindowSettings();

        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
        Closed += OnClosed;
        SourceInitialized += OnSourceInitialized;
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
        refreshTimer.Tick += OnRefreshTimerTick;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        refreshTimer.Start();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (hwndSource is null)
        {
            return;
        }

        SetWindowPos(
            hwndSource.Handle,
            HwndTopmost,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoActivate);
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        if (refreshInProgress)
        {
            return;
        }

        refreshInProgress = true;
        try
        {
            await RefreshAsync();
        }
        finally
        {
            refreshInProgress = false;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Deactivated -= OnDeactivated;
        if (hwndSource is not null)
        {
            hwndSource.RemoveHook(WndProc);
            hwndSource = null;
        }

        refreshTimer.Stop();
        refreshTimer.Tick -= OnRefreshTimerTick;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        if (hwndSource is not null)
        {
            RemoveSystemButtonStyles(hwndSource.Handle);
        }

        hwndSource?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmSysCommand
            && SnapbarWindowInteraction.ShouldBlockSystemCommand((int)wParam.ToInt64()))
        {
            handled = true;
            return IntPtr.Zero;
        }

        if (message == WmNcMouseMove)
        {
            ShowHoverSurface();
            TrackNonClientMouseLeave(hwnd);
            return IntPtr.Zero;
        }

        if (message == WmNcMouseLeave)
        {
            HideHoverSurface();
            return IntPtr.Zero;
        }

        if (message != WmNcHitTest)
        {
            return IntPtr.Zero;
        }

        var screenPoint = new Point((short)(long)lParam, (short)((long)lParam >> 16));
        var localPoint = PointFromScreen(screenPoint);
        var direction = SnapbarWindowInteraction.GetResizeDirection(
            localPoint.X,
            localPoint.Y,
            ActualWidth,
            ActualHeight,
            SnapbarWindowInteraction.DefaultResizeBorder);
        var hitTest = GetHitTestResult(direction);
        if (SnapbarWindowInteraction.ShouldUseClientHitTest(direction))
        {
            ShowHoverSurface();
            handled = true;
            return new IntPtr(HtClient);
        }

        ShowHoverSurface();
        handled = true;
        return new IntPtr(hitTest);
    }

    private static int GetHitTestResult(SnapbarResizeDirection direction)
    {
        return direction switch
        {
            SnapbarResizeDirection.Left => HtLeft,
            SnapbarResizeDirection.TopLeft => HtTopLeft,
            SnapbarResizeDirection.Top => HtTop,
            SnapbarResizeDirection.TopRight => HtTopRight,
            SnapbarResizeDirection.Right => HtRight,
            SnapbarResizeDirection.BottomRight => HtBottomRight,
            SnapbarResizeDirection.Bottom => HtBottom,
            SnapbarResizeDirection.BottomLeft => HtBottomLeft,
            _ => 0,
        };
    }

    private static void RemoveSystemButtonStyles(IntPtr windowHandle)
    {
        var style = GetWindowLongPtr(windowHandle, GwlStyle).ToInt64();
        var updatedStyle = SnapbarWindowInteraction.RemoveSystemButtonStyles(style);
        if (updatedStyle == style)
        {
            return;
        }

        SetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(updatedStyle));
        SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpFrameChanged);
    }

    private async Task RefreshAsync()
    {
        await viewModel.RefreshAsync(CancellationToken.None);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!isUpdatingWindowHeight)
        {
            manualWindowHeight = Math.Max(MinHeight, Height);
        }

        TryUpdateLayoutMetrics();
        PersistWindowSettings();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        PersistWindowSettings();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimelineSnapbarViewModel.Blocks))
        {
            TryUpdateLayoutMetrics();
        }
    }

    private void TryUpdateLayoutMetrics()
    {
        if (isUpdatingLayout)
        {
            return;
        }

        try
        {
            isUpdatingLayout = true;
            UpdateLayoutMetrics();
        }
        catch
        {
            BlocksCanvas.Children.Clear();
            viewModel.ReportDisplayUnavailable();
            UpdateWindowHeight();
        }
        finally
        {
            isUpdatingLayout = false;
        }
    }

    private void UpdateLayoutMetrics()
    {
        var laneCount = viewModel.Blocks.Count == 0 ? 1 : viewModel.Blocks.Max(block => block.Lane) + 1;
        var timelineHeight = TimelineSnapbarLayout.GetTimelineHeight(laneCount);
        var timelineWidth = TimelineGrid.ActualWidth;
        TimelineFadeMask.StartPoint = new Point(0, 0);
        TimelineFadeMask.EndPoint = new Point(timelineWidth, 0);
        BlocksViewport.Width = timelineWidth;
        BlocksViewport.Height = timelineHeight;
        BlocksCanvas.Width = timelineWidth;
        BlocksCanvas.Height = timelineHeight;
        TimelineGrid.Height = timelineHeight;
        var railBounds = TimelineSnapbarLayout.GetRailBounds();
        TimelineRail.Height = railBounds.Height;
        TimelineRail.Margin = new Thickness(0, railBounds.Top, 0, 0);
        var nowLineBounds = TimelineSnapbarLayout.GetNowLineBounds();
        NowLine.Height = nowLineBounds.Height;
        NowLine.Margin = new Thickness(
            timelineWidth * TimelineSnapbarLayout.NowRatio,
            nowLineBounds.Top,
            0,
            0);
        var now = DateTimeOffset.Now;
        NowTimeTextBlock.Text = TimelineTimeDisplay.GetCurrentTime(now);
        NowTimeIndicator.ToolTip = TimelineTimeDisplay.GetDateTooltip(now);
        NowTimeIndicator.Margin = new Thickness(
            0,
            0,
            timelineWidth - (timelineWidth * TimelineSnapbarLayout.NowRatio) + 4,
            timelineHeight - nowLineBounds.Bottom);
        var countdownBaseLeft = timelineWidth * TimelineSnapbarLayout.NowRatio + 8;
        CountdownIndicator.Margin = new Thickness(
            countdownBaseLeft,
            nowLineBounds.Top,
            0,
            0);
        var countdown = TimelineTimeDisplay.GetCountdown(now, viewModel.Blocks);
        CountdownTextBlock.Text = countdown?.Text ?? string.Empty;
        if (countdown is null)
        {
            CountdownIndicator.Visibility = Visibility.Collapsed;
        }
        else
        {
            CountdownIndicator.Visibility = Visibility.Visible;
            CountdownIndicator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var runningBlockBounds = viewModel.Blocks
                .Where(block => block.IsRunning)
                .Select(block =>
                {
                    var bounds = TimelineSnapbarLayout.GetBlockBounds(
                        timelineWidth,
                        block.StartRatio,
                        block.WidthRatio,
                        TimelineSnapbarLayout.MinimumBlockWidth);
                    return new TimelineHorizontalBounds(bounds.Left, bounds.Width);
                });
            var targetBounds = TimelineSnapbarLayout.GetBlockBounds(
                timelineWidth,
                countdown.Target.StartRatio,
                countdown.Target.WidthRatio,
                TimelineSnapbarLayout.MinimumBlockWidth);
            var countdownLeft = TimelineCountdownLayout.GetLeft(
                countdownBaseLeft,
                CountdownIndicator.DesiredSize.Width,
                targetBounds.Left,
                runningBlockBounds);
            AnimateCountdownBase(countdownLeft - countdownBaseLeft);
        }
        UpdateWindowHeight(timelineHeight);
        BlocksCanvas.Children.Clear();

        foreach (var block in viewModel.Blocks)
        {
            var bounds = TimelineSnapbarLayout.GetBlockBounds(
                timelineWidth,
                block.StartRatio,
                block.WidthRatio,
                TimelineSnapbarLayout.MinimumBlockWidth);
            var isHighlighted = TimelineTimeDisplay.IsHighlighted(now, block);
            var button = CreateBlockButton(block, bounds.Width, isHighlighted);
            Canvas.SetLeft(button, bounds.Left);
            Canvas.SetTop(button, TimelineSnapbarLayout.GetBlockTop(block.Lane, laneCount));
            BlocksCanvas.Children.Add(button);
        }

        if (viewModel.AllDayTag is { } allDayTag)
        {
            var calendarWindow = CalendarWindow.Create(now);
            var windowDuration = calendarWindow.End - calendarWindow.Start;
            var bounds = AllDayTagLayout.GetBounds(
                timelineWidth,
                TimelineSnapbarLayout.NowRatio,
                (allDayTag.Start - calendarWindow.Start).TotalMilliseconds / windowDuration.TotalMilliseconds,
                (allDayTag.End - calendarWindow.Start).TotalMilliseconds / windowDuration.TotalMilliseconds);
            var tag = CreateAllDayTag(allDayTag, bounds.Width);
            Canvas.SetLeft(tag, bounds.Left);
            Canvas.SetTop(tag, TimelineSnapbarLayout.GetAllDayTagTop(laneCount));
            BlocksCanvas.Children.Add(tag);
        }
    }

    private void UpdateWindowHeight(double? timelineHeight = null)
    {
        var hasStatus = !string.IsNullOrEmpty(viewModel.StatusText);
        StatusTextBlock.Visibility = hasStatus ? Visibility.Visible : Visibility.Collapsed;
        var requiredHeight = GridVerticalMargin + (timelineHeight ?? TimelineSnapbarLayout.GetTimelineHeight(1))
            + (hasStatus ? StatusRowHeight : 0);
        minimumWindowHeight = requiredHeight;
        var targetHeight = TimelineSnapbarLayout.GetWindowHeight(manualWindowHeight, requiredHeight);

        try
        {
            isUpdatingWindowHeight = true;
            MinHeight = minimumWindowHeight;
            if (Height == targetHeight)
            {
                return;
            }

            Height = targetHeight;
        }
        finally
        {
            isUpdatingWindowHeight = false;
        }
    }

    private void AnimateCountdownBase(double baseX)
    {
        var currentBaseX = CountdownBaseTranslation.X;
        if (currentBaseX == baseX)
        {
            return;
        }

        CountdownWobbleStoryboard.Storyboard.Stop(CountdownIndicator);
        var animation = new DoubleAnimation
        {
            From = currentBaseX,
            To = baseX,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        animation.Completed += (_, _) =>
        {
            CountdownBaseTranslation.X = baseX;
            CountdownBaseTranslation.BeginAnimation(TranslateTransform.XProperty, null);
            CountdownIndicator.BeginStoryboard(
                CountdownWobbleStoryboard.Storyboard,
                HandoffBehavior.SnapshotAndReplace,
                isControllable: true);
        };
        CountdownBaseTranslation.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void RestoreWindowSettings()
    {
        var settings = settingsStore.Load();
        if (settings is null || !settings.IsIntersecting(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight))
        {
            return;
        }

        Width = settings.Width;
        Height = settings.Height;
        manualWindowHeight = Math.Max(MinHeight, Height);
        Left = settings.Left;
        Top = settings.Top;
    }

    private void PersistWindowSettings()
    {
        try
        {
            settingsStore.Save(new SnapbarWindowSettings(Left, Top, Width, Height));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void OnRootMouseEnter(object sender, MouseEventArgs e)
    {
        ShowHoverSurface();
    }

    private void OnRootMouseLeave(object sender, MouseEventArgs e)
    {
        Cursor = null;
        if (!IsCursorWithinWindow())
        {
            HideHoverSurface();
        }
    }

    private void ShowHoverSurface() => HoverSurface.Opacity = 1;

    private void HideHoverSurface() => HoverSurface.Opacity = 0;

    private bool IsCursorWithinWindow()
    {
        return hwndSource is not null
            && GetCursorPos(out var cursor)
            && GetWindowRect(hwndSource.Handle, out var rectangle)
            && SnapbarWindowInteraction.IsWithinBounds(
                cursor.X,
                cursor.Y,
                rectangle.Left,
                rectangle.Top,
                rectangle.Right,
                rectangle.Bottom);
    }

    private static void TrackNonClientMouseLeave(IntPtr windowHandle)
    {
        var eventData = new TrackMouseEventData
        {
            Size = (uint)Marshal.SizeOf<TrackMouseEventData>(),
            Flags = TmeLeave | TmeNonClient,
            WindowHandle = windowHandle,
        };
        TrackMouseEvent(ref eventData);
    }

    private void OnRootPreviewMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        var resizeDirection = SnapbarWindowInteraction.GetResizeDirection(
            position.X,
            position.Y,
            ActualWidth,
            ActualHeight,
            SnapbarWindowInteraction.DefaultResizeBorder);
        Cursor = SnapbarWindowInteraction.ShouldUseMoveCursor(
            IsAppointmentTarget(e.OriginalSource as DependencyObject),
            resizeDirection)
            ? Cursors.SizeAll
            : null;
    }

    private void OnRootPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (SnapbarWindowInteraction.CanBeginDrag(IsAppointmentTarget(e.OriginalSource as DependencyObject))
            && e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static bool IsAppointmentTarget(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is Button)
            {
                return true;
            }
        }

        return false;
    }

    private static Border CreateAllDayTag(AllDayTagViewModel tag, double width)
    {
        var colors = TimelineBubbleColors.Resolve(
            tag.CategoryColors,
            tag.CalendarColor,
            tag.CalendarIdentity);
        var foreground = CreateSolidBrush(colors.Foreground);
        return new Border
        {
            Width = width,
            Height = AllDayTagLayout.TagHeight,
            Background = CreateBubbleFill(colors.LightFill, colors.DarkFill),
            BorderBrush = CreateSolidBrush(colors.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 1),
            ToolTip = CreateAllDayTagTooltip(tag),
            Child = CreateAllDayTagLabel(tag, foreground),
        };
    }

    private static ToolTip CreateAllDayTagTooltip(AllDayTagViewModel tag)
    {
        var titles = new StackPanel();
        foreach (var title in tag.TooltipTitles)
        {
            titles.Children.Add(new TextBlock
            {
                Text = title,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        return new ToolTip
        {
            MaxWidth = 480,
            Content = titles,
        };
    }

    private static Grid CreateAllDayTagLabel(AllDayTagViewModel tag, Brush foreground)
    {
        var label = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
        };
        label.Children.Add(new TextBlock
        {
            Text = tag.Title,
            Foreground = foreground,
            FontWeight = FontWeights.SemiBold,
            FontSize = 9,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var additionalCount = new TextBlock
        {
            Text = $"+{tag.AdditionalCount}",
            Foreground = foreground,
            FontWeight = FontWeights.SemiBold,
            FontSize = 9,
            Margin = new Thickness(4, 0, 0, 0),
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = tag.AdditionalCount > 0 ? Visibility.Visible : Visibility.Collapsed,
        };
        Grid.SetColumn(additionalCount, 1);
        label.Children.Add(additionalCount);

        return label;
    }

    private Button CreateBlockButton(TimelineBlockViewModel block, double width, bool isHighlighted)
    {
        var colors = TimelineBubbleColors.Resolve(
            block.CategoryColors,
            block.CalendarColor,
            block.CalendarIdentity);
        var button = new Button
        {
            DataContext = block,
            Width = width,
            Height = TimelineSnapbarLayout.BubbleHeight,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Style = (Style)FindResource("TimelineBlockButtonStyle"),
            ToolTip = CreateBubbleTooltip(block),
            Content = CreateBubbleContent(block, colors, width, isHighlighted),
        };
        button.Click += OnBlockClick;
        return button;
    }

    private static Border CreateBubbleContent(
        TimelineBlockViewModel block,
        TimelineBubbleColorSet colors,
        double width,
        bool isHighlighted)
    {
        var foreground = CreateSolidBrush(colors.Foreground);
        var content = new Grid();
        content.Children.Add(CreateBubbleLabel(block, foreground, width));
        if (isHighlighted)
        {
            content.Children.Add(CreateHighlightOverlay());
        }

        return new Border
        {
            Background = CreateBubbleFill(colors.LightFill, colors.DarkFill),
            BorderBrush = CreateSolidBrush(colors.Border),
            BorderThickness = isHighlighted ? new Thickness(2) : new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(8, 3, 8, 3),
            Effect = CreateBubbleShadow(block.IsRunning),
            Child = content,
        };
    }

    private static Border CreateHighlightOverlay()
    {
        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(112, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
        };
        overlay.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation
            {
                From = 0.45,
                To = 0.9,
                Duration = TimeSpan.FromSeconds(0.8),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            });
        return overlay;
    }

    private static ToolTip CreateBubbleTooltip(TimelineBlockViewModel block)
    {
        return new ToolTip
        {
            MaxWidth = 480,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = block.Title,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = block.StartTime + "–" + block.End.ToString("HH:mm") + " · " + block.Duration,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = block.TooltipContext,
                        TextWrapping = TextWrapping.Wrap,
                        Visibility = string.IsNullOrWhiteSpace(block.TooltipContext)
                            ? Visibility.Collapsed
                            : Visibility.Visible,
                    },
                },
            },
        };
    }

    private static Grid CreateBubbleLabel(TimelineBlockViewModel block, Brush foreground, double width)
    {
        var label = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(12) },
                new RowDefinition { Height = new GridLength(12) },
            },
        };
        var metadata = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new TextBlock
                {
                    Text = block.StartTime,
                    Foreground = foreground,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    LineHeight = 12,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    TextWrapping = TextWrapping.NoWrap,
                },
                new TextBlock
                {
                    Text = " · ",
                    Foreground = foreground,
                    FontSize = 9,
                    LineHeight = 12,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = TimelineBubbleLayout.ShouldShowDuration(width)
                        ? Visibility.Visible
                        : Visibility.Collapsed,
                },
                new TextBlock
                {
                    Text = block.Duration,
                    Foreground = foreground,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 9,
                    LineHeight = 12,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    TextWrapping = TextWrapping.NoWrap,
                    Visibility = TimelineBubbleLayout.ShouldShowDuration(width)
                        ? Visibility.Visible
                        : Visibility.Collapsed,
                },
            },
        };
        Grid.SetRow(metadata, 0);
        label.Children.Add(metadata);

        var title = new TextBlock
        {
            Text = block.Title,
            Foreground = foreground,
            FontWeight = FontWeights.SemiBold,
            FontSize = 9,
            LineHeight = 12,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(title, 1);
        label.Children.Add(title);

        return label;
    }

    private static LinearGradientBrush CreateBubbleFill(string lightColor, string darkColor)
    {
        return new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(lightColor),
            (Color)ColorConverter.ConvertFromString(darkColor),
            new Point(0, 0),
            new Point(0, 1));
    }

    private static SolidColorBrush CreateSolidBrush(string color)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private static DropShadowEffect CreateBubbleShadow(bool isRunning)
    {
        return new DropShadowEffect
        {
            Color = Colors.Black,
            ShadowDepth = isRunning ? 1 : 0.5,
            BlurRadius = isRunning ? 8 : 4,
            Opacity = isRunning ? 0.45 : 0.25,
        };
    }

    private static void OnBlockClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TimelineBlockViewModel { HasTeamsUrl: true, TeamsUrl: { } url } })
        {
            TimelineSnapbarViewModel.OpenTeamsUrl(url);
        }
    }
}
