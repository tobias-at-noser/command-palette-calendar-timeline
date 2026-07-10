using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CalendarTimeline.Snapbar;

namespace CalendarTimeline.Wpf;

public partial class MainWindow : Window
{
    private const double GridVerticalMargin = 12;
    private const double StatusRowHeight = 16;
    private const double TimelineWidthPadding = 24;
    private const double MinimumBlockWidth = 36;
    private readonly TimelineSnapbarViewModel viewModel;

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
        ResizeMode = ResizeMode.NoResize;
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        UpdateWindowHeight();

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Blocks.CollectionChanged += (_, _) => UpdateLayoutMetrics();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await viewModel.RefreshAsync(CancellationToken.None);
        UpdateLayoutMetrics();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateLayoutMetrics();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimelineSnapbarViewModel.StatusText))
        {
            UpdateLayoutMetrics();
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
        Height = GridVerticalMargin + (timelineHeight ?? TimelineSnapbarLayout.GetTimelineHeight(1))
            + (hasStatus ? StatusRowHeight : 0);
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
