using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SortingVisualizer;

public partial class MainWindow : Window
{
    private MainWindow? _mainWindow;
    private readonly Random _random = new();
    private readonly List<Rectangle> _bars = new();

    private readonly Stopwatch _stopwatch = new();

    private readonly DispatcherTimer _elapsedTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(50)
    };

    private int[] _values = Enumerable.Range(1, 80).ToArray();

    private CancellationTokenSource? _cancellation;
    private TonePlayer? _tonePlayer;
    private MainWindow? _comparisonWindow;

    private int _first = -1;
    private int _second = -1;

    private bool _finished;
    private bool _closing;
    private bool _initialized;

    private Brush _normalBrush = Brushes.DeepSkyBlue;
    private Brush _firstBrush = Brushes.OrangeRed;
    private Brush _secondBrush = Brushes.Gold;
    private Brush _finishedBrush = Brushes.MediumSeaGreen;

    public MainWindow() : this(null, false)
    {
    }

    public MainWindow(int[]? initialValues, bool isComparisonWindow)
    {
        InitializeComponent();

        WaveformBox.ItemsSource = Enum.GetValues<ToneWaveform>();
        WaveformBox.SelectedItem = ToneWaveform.Triangle;

        ConfigureColors();

        if (initialValues is null)
        {
            ShuffleValues();
        }
        else
        {
            _values = (int[])initialValues.Clone();
        }

        BarCountBox.Text = _values.Length.ToString();

        if (isComparisonWindow)
        {
            Title = "Sorting Visualizer — Comparison";
            CompareCheckBox.Visibility = Visibility.Collapsed;
        }

        _initialized = true;
        UpdateColorBrushes();

        _elapsedTimer.Tick += (_, _) => UpdateElapsedText();

        Loaded += (_, _) => DrawBars();

        Closed += (_, _) =>
        {
            _closing = true;

            _elapsedTimer.Stop();
            _stopwatch.Stop();

            _cancellation?.Cancel();

            _tonePlayer?.Dispose();
            _tonePlayer = null;

            _comparisonWindow?.Close();
            _comparisonWindow = null;
        };
    }

    private void ConfigureColors()
    {
        string[] colors =
        {
            "DeepSkyBlue",
            "OrangeRed",
            "Gold",
            "MediumSeaGreen",
            "White",
            "HotPink",
            "Violet",
            "Cyan",
            "Lime",
            "Orange"
        };

        NormalColorBox.ItemsSource = colors;
        FirstColorBox.ItemsSource = colors;
        SecondColorBox.ItemsSource = colors;
        FinishedColorBox.ItemsSource = colors;

        NormalColorBox.SelectedItem = "DeepSkyBlue";
        FirstColorBox.SelectedItem = "OrangeRed";
        SecondColorBox.SelectedItem = "Gold";
        FinishedColorBox.SelectedItem = "MediumSeaGreen";
    }

    private static Brush GetSelectedBrush(
        ComboBox colorBox,
        Brush fallback)
    {
        if (colorBox.SelectedItem is not string name)
            return fallback;

        Color color = (Color)ColorConverter.ConvertFromString(name);

        var brush = new SolidColorBrush(color);
        brush.Freeze();

        return brush;
    }

    private void UpdateColorBrushes()
    {
        _normalBrush = GetSelectedBrush(
            NormalColorBox, Brushes.DeepSkyBlue);

        _firstBrush = GetSelectedBrush(
            FirstColorBox, Brushes.OrangeRed);

        _secondBrush = GetSelectedBrush(
            SecondColorBox, Brushes.Gold);

        _finishedBrush = GetSelectedBrush(
            FinishedColorBox, Brushes.MediumSeaGreen);
    }

    private void Color_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!_initialized)
            return;

        UpdateColorBrushes();
        DrawBars();
    }

    private void ApplyCount_Click(object sender, RoutedEventArgs e)
    {
        if (_cancellation is not null)
            return;

        if (!int.TryParse(BarCountBox.Text, out int count) ||
            count < 2 ||
            count > 1000)
        {
            MessageBox.Show(
                "Enter a whole number between 2 and 1000.",
                "Invalid bar count",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        _values = Enumerable.Range(1, count).ToArray();

        ResetElapsedTime();
        ShuffleValues();
        DrawBars();

        StatusText.Text = $"Created {count} bars";
    }

    private void ShuffleValues()
    {
        // Fisher–Yates shuffle.
        for (int i = _values.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);

            (_values[i], _values[j]) =
                (_values[j], _values[i]);
        }

        _first = -1;
        _second = -1;
        _finished = false;
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        if (_cancellation is not null)
            return;

        ResetElapsedTime();
        ShuffleValues();
        DrawBars();

        StatusText.Text = "Shuffled";
    }

    private async Task StartSortingAsync()
    {
        if (_cancellation is not null)
            return;

        if (AlgorithmBox.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Content is not string algorithm)
        {
            return;
        }

        int[] originalValues = (int[])_values.Clone();
        int[] workingValues = (int[])_values.Clone();

        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;

        SetRunning(true);
        _finished = false;

        int stepCount = 0;

        _stopwatch.Restart();
        UpdateElapsedText();
        _elapsedTimer.Start();

        try
        {
            IEnumerable<SortStep> steps = algorithm switch
            {
                "Bubble Sort" =>
                    SortingAlgorithms.BubbleSort(workingValues),

                "Selection Sort" =>
                    SortingAlgorithms.SelectionSort(workingValues),

                "Insertion Sort" =>
                    SortingAlgorithms.InsertionSort(workingValues),

                "Merge Sort" =>
                    SortingAlgorithms.MergeSort(workingValues),

                "Quick Sort" =>
                    SortingAlgorithms.QuickSort(workingValues),

                _ => throw new InvalidOperationException(
                    "Unknown algorithm.")
            };

            foreach (SortStep step in steps)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                _values = step.Values;
                _first = step.First;
                _second = step.Second;

                DrawBars();
                UpdateSound();

                stepCount++;

                StatusText.Text =
                    $"{algorithm} — steps: {stepCount}";

                await Task.Delay(
                    (int)DelaySlider.Value,
                    cancellation.Token);
            }

            cancellation.Token.ThrowIfCancellationRequested();

            _values = (int[])workingValues.Clone();
            _finished = true;

            StatusText.Text =
                $"{algorithm} complete — {stepCount} visualization steps";
        }
        catch (OperationCanceledException)
        {
            // Restore the original values, including after an
            // interrupted Merge Sort copy operation.
            _values = originalValues;
            _finished = false;

            if (!_closing)
            {
                StatusText.Text =
                    "Stopped — starting arrangement restored";
            }
        }
        catch (Exception ex)
        {
            _values = originalValues;
            _finished = false;

            if (!_closing)
            {
                StatusText.Text = "Sorting failed";

                MessageBox.Show(
                    ex.Message,
                    "Sorting error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        finally
        {
            _stopwatch.Stop();
            _elapsedTimer.Stop();

            _tonePlayer?.Mute();

            _first = -1;
            _second = -1;

            cancellation.Dispose();
            _cancellation = null;

            if (!_closing)
            {
                UpdateElapsedText();
                SetRunning(false);
                DrawBars();
            }
        }
    }
    
    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        // The original window points to the comparison window.
        // The comparison window points back to the original.
        MainWindow? partner = _comparisonWindow ?? _mainWindow;

        if (_closing || _cancellation is not null)
            return;

        // Wait until both windows are idle before starting a paired run.
        if (partner is not null &&
            (partner._closing || partner._cancellation is not null))
        {
            StatusText.Text =
                "Stop the other visualizer before starting both.";

            return;
        }

        if (partner is null)
        {
            await StartSortingAsync();
        }
        else
        {
            // Each method runs until its first await, then both
            // animations continue independently.
            await Task.WhenAll(
                StartSortingAsync(),
                partner.StartSortingAsync());
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _tonePlayer?.Mute();
        _cancellation?.Cancel();
    }

    private void SetRunning(bool running)
    {
        StartButton.IsEnabled = !running;
        ShuffleButton.IsEnabled = !running;
        AlgorithmBox.IsEnabled = !running;

        BarCountBox.IsEnabled = !running;
        ApplyCountButton.IsEnabled = !running;

        CompareCheckBox.IsEnabled = !running;

        StopButton.IsEnabled = running;

        // Delay, pitch, waveform, colors, and sound remain adjustable.
    }

    private void UpdateElapsedText()
    {
        ElapsedText.Text =
            $"Time: {_stopwatch.Elapsed.TotalSeconds:F2} s";
    }

    private void ResetElapsedTime()
    {
        _elapsedTimer.Stop();
        _stopwatch.Reset();

        UpdateElapsedText();
    }

    private void UpdateSound()
    {
        if (SoundCheckBox.IsChecked != true)
        {
            _tonePlayer?.Mute();
            return;
        }

        int index = _first >= 0 ? _first : _second;

        if (index < 0 || index >= _values.Length)
        {
            _tonePlayer?.Mute();
            return;
        }

        ToneWaveform waveform =
            WaveformBox.SelectedItem is ToneWaveform selected
                ? selected
                : ToneWaveform.Triangle;

        if (_tonePlayer is null)
        {
            try
            {
                _tonePlayer = new TonePlayer
                {
                    Waveform = waveform,
                    PitchMultiplier = PitchSlider.Value
                };
            }
            catch (Exception ex)
            {
                SoundCheckBox.IsChecked = false;

                MessageBox.Show(
                    $"Audio could not be started:\n{ex.Message}",
                    "Audio unavailable");

                return;
            }
        }

        _tonePlayer.Waveform = waveform;
        _tonePlayer.PitchMultiplier = PitchSlider.Value;

        _tonePlayer.PlayValue(
            _values[index],
            _values.Length);
    }

    private void SoundCheckBox_Unchecked(
        object sender,
        RoutedEventArgs e)
    {
        _tonePlayer?.Mute();
    }

    private void WaveformBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_tonePlayer is not null &&
            WaveformBox.SelectedItem is ToneWaveform waveform)
        {
            _tonePlayer.Waveform = waveform;
        }
    }

    private void PitchSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_tonePlayer is not null)
        {
            _tonePlayer.PitchMultiplier = e.NewValue;
        }
    }

    private void CompareCheckBox_Checked(
        object sender,
        RoutedEventArgs e)
    {
        if (!_initialized || _comparisonWindow is not null)
            return;

        var comparison = new MainWindow(_values, true);
        comparison._mainWindow = this;

        comparison.DelaySlider.Value = DelaySlider.Value;
        comparison.PitchSlider.Value = PitchSlider.Value;
        comparison.WaveformBox.SelectedItem = WaveformBox.SelectedItem;

        comparison.NormalColorBox.SelectedItem =
            NormalColorBox.SelectedItem;

        comparison.FirstColorBox.SelectedItem =
            FirstColorBox.SelectedItem;

        comparison.SecondColorBox.SelectedItem =
            SecondColorBox.SelectedItem;

        comparison.FinishedColorBox.SelectedItem =
            FinishedColorBox.SelectedItem;

        // Keep the second window silent initially to avoid two
        // tones playing over each other.
        comparison.SoundCheckBox.IsChecked = false;

        comparison.AlgorithmBox.SelectedIndex =
            AlgorithmBox.SelectedIndex == 4
                ? 0
                : AlgorithmBox.SelectedIndex + 1;

        _comparisonWindow = comparison;

        comparison.Closed += (_, _) =>
        {
            _comparisonWindow = null;

            if (!_closing)
            {
                CompareCheckBox.IsChecked = false;
            }
        };

        // Arrange both windows on the primary screen.
        Rect workArea = SystemParameters.WorkArea;
        double halfWidth = workArea.Width / 2;

        WindowState = WindowState.Normal;
        WindowStartupLocation = WindowStartupLocation.Manual;

        Left = workArea.Left;
        Top = workArea.Top;
        Width = halfWidth;
        Height = workArea.Height;

        comparison.WindowStartupLocation =
            WindowStartupLocation.Manual;

        comparison.Left = workArea.Left + halfWidth;
        comparison.Top = workArea.Top;
        comparison.Width = halfWidth;
        comparison.Height = workArea.Height;

        comparison.Show();

        StatusText.Text =
            "Comparison opened with the same starting bars";
    }

    private void CompareCheckBox_Unchecked(
        object sender,
        RoutedEventArgs e)
    {
        MainWindow? comparison = _comparisonWindow;
        _comparisonWindow = null;

        comparison?.Close();
    }

    private void BarsCanvas_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        DrawBars();
    }

    private void DrawBars()
    {
        if (!_initialized ||
            BarsCanvas.ActualWidth <= 0 ||
            BarsCanvas.ActualHeight <= 0)
        {
            return;
        }

        // Recreate rectangles only when the bar count changes.
        if (_bars.Count != _values.Length)
        {
            BarsCanvas.Children.Clear();
            _bars.Clear();

            for (int i = 0; i < _values.Length; i++)
            {
                var rectangle = new Rectangle();

                _bars.Add(rectangle);
                BarsCanvas.Children.Add(rectangle);
            }
        }

        double slotWidth =
            BarsCanvas.ActualWidth / _values.Length;

        double availableHeight =
            Math.Max(1, BarsCanvas.ActualHeight - 8);

        int maximum = _values.Length;

        // Narrow bars have no gaps.
        double gap = slotWidth >= 3 ? 1 : 0;

        for (int i = 0; i < _values.Length; i++)
        {
            Rectangle bar = _bars[i];

            double height =
                (double)_values[i] / maximum * availableHeight;

            bar.Width = slotWidth - gap;
            bar.Height = height;

            if (_finished)
                bar.Fill = _finishedBrush;
            else if (i == _first)
                bar.Fill = _firstBrush;
            else if (i == _second)
                bar.Fill = _secondBrush;
            else
                bar.Fill = _normalBrush;

            Canvas.SetLeft(bar, i * slotWidth);

            Canvas.SetTop(
                bar,
                BarsCanvas.ActualHeight - height);
        }
    }
}