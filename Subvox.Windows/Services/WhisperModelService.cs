using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace Subvox.Windows.Services;

public sealed class WhisperModelService
{
    public WhisperModelService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        ModelDirectory = Path.Combine(localAppData, "Subvox", "models");
        ModelPath = Path.Combine(ModelDirectory, "ggml-base.en.bin");
    }

    public string ModelDirectory { get; }

    public string ModelPath { get; }

    public bool IsModelReady => File.Exists(ModelPath) && new FileInfo(ModelPath).Length > 1_000_000;

    public async Task DownloadAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(ModelDirectory);
        var partialPath = ModelPath + ".partial";

        try
        {
            await using (var modelStream = await WhisperGgmlDownloader.Default
                .GetGgmlModelAsync(GgmlType.BaseEn, cancellationToken: cancellationToken))
            await using (var output = File.Create(partialPath))
            {
                var buffer = new byte[128 * 1024];
                long copied = 0;
                int bytesRead;

                while ((bytesRead = await modelStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    copied += bytesRead;
                    if (modelStream.CanSeek && modelStream.Length > 0)
                    {
                        progress?.Report((double)copied / modelStream.Length);
                    }
                }

                await output.FlushAsync(cancellationToken);
            }

            File.Move(partialPath, ModelPath, overwrite: true);
            progress?.Report(1d);
        }
        catch
        {
            if (File.Exists(partialPath))
            {
                File.Delete(partialPath);
            }

            throw;
        }
    }

    public async Task<string> TranscribeAsync(string wavePath, CancellationToken cancellationToken)
    {
        if (!IsModelReady)
        {
            throw new InvalidOperationException("The local Whisper model is not ready.");
        }

        using var factory = WhisperFactory.FromPath(ModelPath);
        using var processor = factory.CreateBuilder()
            .WithLanguage("en")
            .Build();
        await using var audio = File.OpenRead(wavePath);
        var transcript = new StringBuilder();

        await foreach (var segment in processor.ProcessAsync(audio, cancellationToken))
        {
            transcript.Append(segment.Text);
        }

        return transcript.ToString().Trim();
    }
}
