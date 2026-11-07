using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Subvox.Windows.Models;
using Subvox.Windows.Services;

namespace Subvox.Windows;

public partial class MainWindow : Window
{
    private static readonly Brush IdleBrush = new SolidColorBrush(Color.FromRgb(127, 141, 161));
    private static readonly Brush ReadyBrush = new SolidColorBrush(Color.FromRgb(89, 216, 197));
    private static readonly Brush RecordingBrush = new SolidColorBrush(Color.FromRgb(255, 116, 116));
    private static readonly Brush BusyBrush = new SolidColorBrush(Color.FromRgb(102, 178, 255));
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 184, 92));

    private readonly AudioRecorder recorder = new();
    private readonly WhisperModelService whisper = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly Stopwatch recordingClock = new();
    private CancellationTokenSource? operationCancellation;
    private string? currentRecordingPath;
    private bool isBusy;

    public MainWindow()
    {
        InitializeComponent();
        recorder.LevelChanged += Recorder_LevelChanged;
        timer.Tick += Timer_Tick;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshDevices();
        UpdateModelState();
    }

    private void RefreshDevices()
    {
        var previousDeviceNumber = (InputDeviceBox.SelectedItem as AudioInputDevice)?.DeviceNumber;
        var devices = AudioDeviceCatalog.GetInputDevices();
        InputDeviceBox.ItemsSource = devices;
        InputDeviceBox.SelectedItem = devices.FirstOrDefault(device => device.DeviceNumber == previousDeviceNumber)
            ?? AudioDevicePreference.PickPreferred(devices);

        if (devices.Count == 0)
        {
            SetStatus("No microphone is available. Connect Aeropex in Windows Bluetooth settings, then refresh.", ErrorBrush);
        }
        else if (InputDeviceBox.SelectedItem is AudioInputDevice selected && selected.IsAeropex)
        {
            SetStatus($"Aeropex microphone selected: {selected.Name}", ReadyBrush);
        }
        else
        {
            SetStatus("Microphone ready. Connect Aeropex and refresh if you want to use its headset mic.", IdleBrush);
        }
    }

    private void UpdateModelState(bool updateStatus = true)
    {
        var ready = whisper.IsModelReady;
        ModelStatusText.Text = ready ? "base.en is ready for offline transcription" : "base.en is not downloaded yet (about 142 MB)";
        DownloadModelButton.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
        RecordButton.IsEnabled = ready && InputDeviceBox.SelectedItem is not null && !isBusy;

        if (updateStatus && ready && InputDeviceBox.SelectedItem is not null && !recorder.IsRecording)
        {
            SetStatus("Ready. Speak quietly and keep the Aeropex microphone unobstructed.", ReadyBrush);
        }
    }

    private async void DownloadModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        operationCancellation = new CancellationTokenSource();
        DownloadModelButton.IsEnabled = false;
        RefreshDevicesButton.IsEnabled = false;
        RecordButton.IsEnabled = false;
        ModelStatusText.Text = "Downloading base.en…";
        var progress = new Progress<double>(value => ModelStatusText.Text = $"Downloading base.en… {Math.Round(value * 100)}%");

        try
        {
            SetStatus("Downloading the local Whisper model once. Audio will remain on this PC.", BusyBrush);
            await whisper.DownloadAsync(progress, operationCancellation.Token);
            SetStatus("Local Whisper model downloaded. Ready to record.", ReadyBrush);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Model download canceled.", IdleBrush);
        }
        catch (Exception exception)
        {
            SetStatus($"Model download failed: {exception.Message}", ErrorBrush);
        }
        finally
        {
            isBusy = false;
            DownloadModelButton.IsEnabled = true;
            RefreshDevicesButton.IsEnabled = true;
            operationCancellation.Dispose();
            operationCancellation = null;
            UpdateModelState(updateStatus: false);
        }
    }

    private async void RecordButton_Click(object sender, RoutedEventArgs e) => await ToggleRecordingAsync();

    private async Task ToggleRecordingAsync()
    {
        if (isBusy)
        {
            return;
        }

        if (recorder.IsRecording)
        {
            await StopAndTranscribeAsync();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        if (!whisper.IsModelReady)
        {
            SetStatus("Download the local Whisper model before recording.", ErrorBrush);
            return;
        }

        if (InputDeviceBox.SelectedItem is not AudioInputDevice device)
        {
            SetStatus("Choose a microphone before recording.", ErrorBrush);
            return;
        }

        try
        {
            var recordingsDirectory = Path.Combine(Path.GetTempPath(), "Subvox");
            currentRecordingPath = Path.Combine(recordingsDirectory, $"whisper-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.wav");
            recorder.Start(device.DeviceNumber, currentRecordingPath);
            recordingClock.Restart();
            timer.Start();
            RecordButton.Content = "Stop and transcribe  ·  F8";
            RecordButton.Background = new SolidColorBrush(Color.FromRgb(121, 45, 53));
            InputDeviceBox.IsEnabled = false;
            RefreshDevicesButton.IsEnabled = false;
            DownloadModelButton.IsEnabled = false;
            SetStatus($"Listening through {device.Name}. Whisper naturally; press F8 when finished.", RecordingBrush);
        }
        catch (Exception exception)
        {
            SetStatus($"Could not start that microphone: {exception.Message}", ErrorBrush);
            DeleteCurrentRecording();
        }
    }

    private async Task StopAndTranscribeAsync()
    {
        isBusy = true;
        timer.Stop();
        recordingClock.Stop();
        RecordButton.IsEnabled = false;
        SetStatus("Transcribing locally with Whisper…", BusyBrush);

        try
        {
            await recorder.StopAsync();
            if (currentRecordingPath is null)
            {
                throw new InvalidOperationException("The temporary recording path was lost.");
            }

            operationCancellation = new CancellationTokenSource();
            var text = await whisper.TranscribeAsync(currentRecordingPath, operationCancellation.Token);
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("No speech was detected. Move the microphone slightly closer and try again.", ErrorBrush);
            }
            else
            {
                if (TranscriptBox.Text.Length > 0 && !TranscriptBox.Text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                {
                    TranscriptBox.AppendText(Environment.NewLine);
                }

                TranscriptBox.AppendText(text);
                TranscriptBox.ScrollToEnd();
                SetStatus("Transcription complete. Review or copy the text.", ReadyBrush);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Transcription canceled.", IdleBrush);
        }
        catch (Exception exception)
        {
            SetStatus($"Transcription failed: {exception.Message}", ErrorBrush);
        }
        finally
        {
            DeleteCurrentRecording();
            operationCancellation?.Dispose();
            operationCancellation = null;
            isBusy = false;
            InputLevelBar.Value = 0;
            TimerText.Text = "00:00";
            RecordButton.Content = "Start whispering  ·  F8";
            RecordButton.Background = new SolidColorBrush(Color.FromRgb(30, 107, 99));
            InputDeviceBox.IsEnabled = true;
            RefreshDevicesButton.IsEnabled = true;
            DownloadModelButton.IsEnabled = true;
            UpdateModelState(updateStatus: false);
        }
    }

    private void Recorder_LevelChanged(object? sender, double level)
    {
        Dispatcher.BeginInvoke(() => InputLevelBar.Value = Math.Min(100, level * 240));
    }

    private void Timer_Tick(object? sender, EventArgs e) => TimerText.Text = recordingClock.Elapsed.ToString(@"mm\:ss");

    private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TranscriptBox.Text))
        {
            Clipboard.SetText(TranscriptBox.Text);
            SetStatus("Transcript copied to the clipboard.", ReadyBrush);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        TranscriptBox.Clear();
        SetStatus("Transcript cleared.", IdleBrush);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F8)
        {
            e.Handled = true;
            await ToggleRecordingAsync();
        }
    }

    private void SetStatus(string message, Brush color)
    {
        StatusText.Text = message;
        StatusDot.Fill = color;
    }

    private void DeleteCurrentRecording()
    {
        var path = currentRecordingPath;
        currentRecordingPath = null;
        if (path is null || !File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup; the file remains in the user's temporary directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup; the file remains in the user's temporary directory.
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        operationCancellation?.Cancel();
        recorder.Dispose();
        DeleteCurrentRecording();
    }
}
