using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CalendarTimeline.Snapbar;

namespace CalendarTimeline.Wpf;

public partial class MainWindow : Window
{
    private const double GridVerticalMargin = 6;
    private const double StatusRowHeight = 16;
    private const double TimelineWidthPadding = 24;
    private const double MinimumBlockWidth = 36;
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
    private const uint SwpFrameChanged = 0x0020;
    private readonly TimelineSnapbarViewModel viewModel;
    private readonly SnapbarWindowSettingsStore settingsStore = new();
    private readonly DispatcherTimer refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
    private bool refreshInProgress;
    private bool isUpdatingLayout;
    private double minimumWindowHeight;
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
        UpdateWindowHeight();
        RestoreWindowSettings();

        Loaded += OnLoaded;
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
        var timelineWidth = Math.Max(0, ActualWidth - TimelineWidthPadding);
        BlocksCanvas.Height = timelineHeight;
        TimelineGrid.Height = timelineHeight;
        TimelineRail.Height = TimelineSnapbarLayout.RailHeight;
        NowLine.Height = timelineHeight;
        NowLine.Margin = new Thickness(timelineWidth * TimelineSnapbarLayout.NowRatio, 0, 0, 0);
        UpdateWindowHeight(timelineHeight);
        BlocksCanvas.Children.Clear();

        foreach (var block in viewModel.Blocks)
        {
            var bounds = TimelineSnapbarLayout.GetBlockBounds(
                timelineWidth,
                block.StartRatio,
                block.WidthRatio,
                MinimumBlockWidth);
            var button = CreateBlockButton(block, bounds.Width);
            Canvas.SetLeft(button, bounds.Left);
            Canvas.SetTop(button, TimelineSnapbarLayout.GetBlockTop(block.Lane, laneCount));
            BlocksCanvas.Children.Add(button);
        }
    }

    private void UpdateWindowHeight(double? timelineHeight = null)
    {
        var hasStatus = !string.IsNullOrEmpty(viewModel.StatusText);
        StatusTextBlock.Visibility = hasStatus ? Visibility.Visible : Visibility.Collapsed;
        var requiredHeight = GridVerticalMargin + (timelineHeight ?? TimelineSnapbarLayout.GetTimelineHeight(1))
            + (hasStatus ? StatusRowHeight : 0);
        minimumWindowHeight = requiredHeight;
        Height = TimelineSnapbarLayout.GetWindowHeight(Height, requiredHeight);
        MinHeight = minimumWindowHeight;
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

    private static Button CreateBlockButton(TimelineBlockViewModel block, double width)
    {
        var button = new Button
        {
            DataContext = block,
            Width = width,
            Height = TimelineSnapbarLayout.BubbleHeight,
            Padding = new Thickness(8, 0, 8, 0),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(block.IsRunning ? "#FF2E8B57" : "#FF336699")),
            ToolTip = new TextBlock { Text = block.Subtitle },
            Content = new TextBlock
            {
                Text = FormatBubbleText(block),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                LineHeight = 16,
                TextTrimming = TextTrimming.CharacterEllipsis,
            },
        };
        button.Click += OnBlockClick;
        return button;
    }

    private static string FormatBubbleText(TimelineBlockViewModel block)
    {
        var time = block.Subtitle.Split(" · ", 2, StringSplitOptions.None)[0];
        return $"{block.Title} · {time}";
    }

    private static void OnBlockClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TimelineBlockViewModel { HasTeamsUrl: true, TeamsUrl: { } url } })
        {
            TimelineSnapbarViewModel.OpenTeamsUrl(url);
        }
    }
}
