using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CalendarTimeline.Snapbar;

namespace CalendarTimeline.Wpf;

public partial class MainWindow : Window
{
    private const double WindowHeight = 96;
    private const double LaneHeight = 32;
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
        Height = WindowHeight;

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
        NowLine.Margin = new Thickness(timelineWidth / 2, 4, 0, 4);
        BlocksCanvas.Children.Clear();

        foreach (var block in viewModel.Blocks)
        {
            var button = CreateBlockButton(block, timelineWidth);
            Canvas.SetLeft(button, timelineWidth * block.StartRatio);
            Canvas.SetTop(button, block.Lane * LaneHeight);
            BlocksCanvas.Children.Add(button);
        }
    }

    private static Button CreateBlockButton(TimelineBlockViewModel block, double timelineWidth)
    {
        var button = new Button
        {
            DataContext = block,
            Width = Math.Max(MinimumBlockWidth, timelineWidth * block.WidthRatio),
            Height = 28,
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(block.IsRunning ? "#FF2E8B57" : "#FF336699")),
            ToolTip = new TextBlock { Text = block.Subtitle },
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = block.Title,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                    },
                    new TextBlock
                    {
                        Text = block.Subtitle,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD5E3F0")),
                        FontSize = 11,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                    },
                },
            },
        };
        button.Click += OnBlockClick;
        return button;
    }

    private static void OnBlockClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TimelineBlockViewModel { HasTeamsUrl: true, TeamsUrl: { } url } })
        {
            TimelineSnapbarViewModel.OpenTeamsUrl(url);
        }
    }
}
