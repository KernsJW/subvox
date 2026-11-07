# Subvox

Subvox is a Windows whisper-dictation app designed around an Aeropex/AfterShokz/Shokz headset. It automatically prefers the headset microphone, shows the live input level, records a short quiet passage, and transcribes it locally with Whisper.

The Aeropex transducers use bone conduction for **playback**. Its Windows microphone is still a conventional Bluetooth headset microphone, so Subvox can recognize an audible whisper but cannot decode completely silent mouthing or read jaw vibrations.

## Windows build and run

Requirements:

- Windows 11
- .NET 10 SDK for building (`dotnet --version`)
- Aeropex paired in **Settings → Bluetooth & devices** and allowed as a microphone
- Microsoft Visual C++ 2022 Redistributable, required by the Whisper CPU runtime

From PowerShell:

```powershell
dotnet restore .\Subvox.Windows.slnx
dotnet build .\Subvox.Windows.slnx --no-restore
dotnet run --project .\Subvox.Windows\Subvox.Windows.csproj
```

On first launch, choose **Download local model**. Subvox downloads Whisper `base.en` (about 142 MB) to `%LOCALAPPDATA%\Subvox\models`. Recording and transcription then remain on the PC. Temporary WAV files are removed after each transcription or normal app close.

## Using Aeropex whisper mode

1. Pair Aeropex in Windows and confirm its headset microphone is available.
2. Launch Subvox. It prefers device names containing `Aeropex`, `AfterShokz`, or `Shokz`.
3. Download the local model once.
4. Select **Start whispering** or press **F8**, speak quietly, then press F8 again.
5. Review, edit, or copy the transcript.

Selecting a Bluetooth headset microphone switches the headset into the lower-bandwidth HFP call profile while recording. That is expected Bluetooth behavior.

## Local checks

```powershell
dotnet build .\Subvox.Windows.slnx
dotnet run --project .\Subvox.Windows.Tests\Subvox.Windows.Tests.csproj
```

The deterministic checks cover Aeropex/Shokz microphone preference and fallback selection. Physical headset capture and quiet-speech accuracy still require owner UAT with the actual Aeropex.

## Repository layout

- `Subvox.Windows/` — current Windows WPF application.
- `Subvox.Windows.Tests/` — dependency-free Windows verification runner.
- `docs/windows-ui-brief.md` — UI references, states, and hardware boundary.
- `Subvox/`, `SubvoxTests/`, `project.yml` — the original macOS headphone-motion research prototype retained for reference.

The macOS experiment tests motion-template classification with supported Apple headphones. It is separate from Windows Aeropex whisper dictation and is not required to build or run the Windows app.
