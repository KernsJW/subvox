using NAudio.Wave;

namespace Subvox.Windows.Services;

public sealed class AudioRecorder : IDisposable
{
    private WaveIn? capture;
    private WaveFileWriter? writer;
    private TaskCompletionSource? stopCompletion;

    public event EventHandler<double>? LevelChanged;

    public bool IsRecording => capture is not null;

    public void Start(int deviceNumber, string outputPath)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("A recording is already in progress.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        stopCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        capture = new WaveIn
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(16_000, 16, 1),
            BufferMilliseconds = 100
        };
        writer = new WaveFileWriter(outputPath, capture.WaveFormat);
        capture.DataAvailable += OnDataAvailable;
        capture.RecordingStopped += OnRecordingStopped;

        try
        {
            capture.StartRecording();
        }
        catch
        {
            CleanupCapture();
            throw;
        }
    }

    public Task StopAsync()
    {
        if (capture is null)
        {
            return Task.CompletedTask;
        }

        capture.StopRecording();
        return stopCompletion?.Task ?? Task.CompletedTask;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs args)
    {
        writer?.Write(args.Buffer, 0, args.BytesRecorded);

        var peak = 0d;
        for (var offset = 0; offset + 1 < args.BytesRecorded; offset += 2)
        {
            var sample = BitConverter.ToInt16(args.Buffer, offset) / 32768d;
            peak = Math.Max(peak, Math.Abs(sample));
        }

        LevelChanged?.Invoke(this, Math.Clamp(peak, 0d, 1d));
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs args)
    {
        var completion = stopCompletion;
        CleanupCapture();

        if (args.Exception is null)
        {
            completion?.TrySetResult();
        }
        else
        {
            completion?.TrySetException(args.Exception);
        }
    }

    private void CleanupCapture()
    {
        if (capture is not null)
        {
            capture.DataAvailable -= OnDataAvailable;
            capture.RecordingStopped -= OnRecordingStopped;
            capture.Dispose();
            capture = null;
        }

        writer?.Dispose();
        writer = null;
    }

    public void Dispose()
    {
        if (capture is not null)
        {
            capture.StopRecording();
        }

        CleanupCapture();
    }
}
