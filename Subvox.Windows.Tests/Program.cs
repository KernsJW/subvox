using Subvox.Windows.Models;
using Subvox.Windows.Services;

if (args.Contains("--model-smoke", StringComparer.OrdinalIgnoreCase))
{
    var model = new WhisperModelService();
    if (!model.IsModelReady)
    {
        var progress = new Progress<double>(value => Console.WriteLine($"model-download {value:P0}"));
        await model.DownloadAsync(progress, CancellationToken.None);
    }

    Console.WriteLine($"Whisper model ready: {model.ModelPath}");
    return 0;
}

var transcribeIndex = Array.FindIndex(args, value => value.Equals("--transcribe", StringComparison.OrdinalIgnoreCase));
if (transcribeIndex >= 0)
{
    if (transcribeIndex + 1 >= args.Length)
    {
        Console.Error.WriteLine("--transcribe requires a WAV path.");
        return 2;
    }

    var model = new WhisperModelService();
    if (!model.IsModelReady)
    {
        await model.DownloadAsync(null, CancellationToken.None);
    }

    var transcript = await model.TranscribeAsync(args[transcribeIndex + 1], CancellationToken.None);
    Console.WriteLine(transcript);
    return string.IsNullOrWhiteSpace(transcript) ? 1 : 0;
}

var failures = new List<string>();

Check(AudioDevicePreference.IsPreferredName("Headset Microphone (Aeropex by AfterShokz)"), "Aeropex name should be preferred");
Check(AudioDevicePreference.IsPreferredName("Microphone (SHOKZ OpenRun)"), "Shokz name should be preferred case-insensitively");
Check(!AudioDevicePreference.IsPreferredName("Built-in microphone"), "Unrelated microphone should not be preferred");
Check(!AudioDevicePreference.IsPreferredName(null), "Missing device name should not be preferred");

var builtIn = new AudioInputDevice(0, "Built-in microphone");
var aeropex = new AudioInputDevice(1, "Aeropex by AfterShokz");
Check(AudioDevicePreference.PickPreferred([builtIn, aeropex]) == aeropex, "Aeropex should win even when it is not first");
Check(AudioDevicePreference.PickPreferred([builtIn]) == builtIn, "First device should be the fallback");
Check(AudioDevicePreference.PickPreferred([]) is null, "Empty device list should have no selection");

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} verification check(s) failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine("All 7 Subvox Windows verification checks passed.");
return 0;

void Check(bool condition, string message)
{
    if (!condition)
    {
        failures.Add(message);
    }
}
