using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CalendarTimeline.Snapbar;

namespace CalendarTimeline.Wpf;

public partial class MainWindow : Window
{
    private const double LaneHeight = 24;
    private const double TimelinePadding = 8;
    private const double StatusHeight = 16;
    private const double TimelineWidthPadding = 24;
    private const double MinimumBlockWidth = 36;
    private const double NowRatio = 1d / 9d;
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
        var timelineWidth = Math.Max(0, ActualWidth - TimelineWidthPadding);
        NowLine.Margin = new Thickness(timelineWidth * NowRatio, 2, 0, 2);
        UpdateWindowHeight();
        BlocksCanvas.Children.Clear();

        foreach (var block in viewModel.Blocks)
        {
            var button = CreateBlockButton(block, timelineWidth);
            Canvas.SetLeft(button, timelineWidth * block.StartRatio);
            Canvas.SetTop(button, Math.Max(0, BlocksCanvas.Height - TimelinePadding - (block.Lane + 1) * LaneHeight));
            BlocksCanvas.Children.Add(button);
        }
    }

    private void UpdateWindowHeight()
    {
        var laneCount = viewModel.Blocks.Count == 0 ? 1 : viewModel.Blocks.Max(block => block.Lane) + 1;
        var timelineHeight = TimelinePadding + laneCount * LaneHeight;
        BlocksCanvas.Height = timelineHeight;
        TimelineGrid.Height = timelineHeight;
        Height = 12 + timelineHeight + (string.IsNullOrEmpty(viewModel.StatusText) ? 0 : StatusHeight);
    }

    private static Button CreateBlockButton(TimelineBlockViewModel block, double timelineWidth)
    {
        var button = new Button
        {
            DataContext = block,
            Width = Math.Max(MinimumBlockWidth, timelineWidth * block.WidthRatio),
            Height = LaneHeight - 2,
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(block.IsRunning ? "#FF2E8B57" : "#FF336699")),
            ToolTip = new TextBlock { Text = block.Subtitle },
            Content = new TextBlock
            {
                Text = FormatBubbleText(block),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
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
