# Subvox

A macOS whisper-dictation app and research prototype. It can turn quiet speech from an Aeropex/AfterShokz/Shokz microphone into text, and separately tests whether compatible headphone-motion data (`CMHeadphoneMotionManager`) can turn **silently mouthed words into text**.

The experimental motion decoder does not use a language model. It is a small-vocabulary template recognizer plus a statistical test that tells you whether the signal is real. Whisper mode uses Apple's Speech framework.

## Build & run

```sh
brew install xcodegen   # once
xcodegen generate
open Subvox.xcodeproj   # ⌘R
```

Or from the command line:

```sh
xcodebuild -project Subvox.xcodeproj -scheme Subvox -derivedDataPath build -allowProvisioningUpdates build
open build/Build/Products/Debug/Subvox.app
xcodebuild -project Subvox.xcodeproj -scheme Subvox -derivedDataPath build test
```

Requires macOS 14+. Whisper mode works with Aeropex or any Mac microphone; pair the headset in Bluetooth settings before launching, then grant **Microphone** and **Speech Recognition** when prompted. The experimental motion screens require AirPods Pro or another headphone with dynamic head tracking and request **Motion** only when you open one of those screens.

## Aeropex whisper mode

1. Pair **Aeropex by AfterShokz** in macOS Bluetooth settings.
2. Open **Whisper**. Subvox automatically prefers an input whose name contains Aeropex, AfterShokz, or Shokz.
3. Choose **Start Whispering** and speak quietly. The live meter confirms that the microphone is receiving audio.
4. Copy the resulting transcript when finished.

Aeropex uses bone conduction for speaker output, but its exposed input is a conventional Bluetooth headset microphone. It does not expose jaw-vibration or head-motion sensor data to macOS, so it can transcribe an audible whisper but cannot decode silent mouthing. Using its microphone also switches Bluetooth audio to the call-quality HFP profile.

## Screens

| Screen | What it does |
|---|---|
| **Whisper** | Transcribes quiet speech from Aeropex or another selected microphone. No motion sensor or training required. |
| **Sensors** | Live plots of user acceleration, rotation rate and attitude, the measured sample rate, a mic input picker and level meter, and a recorder for experiment sessions. |
| **Train** | Pick a word, hold **Space**, mouth it, release. Each take is stored as a template. **Evaluate** runs leave-one-out classification and a 500× label-permutation test, then shows accuracy against chance, a p-value and a confusion matrix. The feature checkboxes (acceleration / rotation / attitude) let you test which channels carry the signal. |
| **Decode** | The motion-to-text screen. Hold Space, or use auto-detect after calibrating the noise floor. Each segment is DTW-matched against your templates and added to the transcript. A live mic transcript runs alongside it for comparison. |

## How decoding works

1. **Segmentation** (`CaptureController`). Space-hold, or an energy trigger above a calibrated noise floor with pre-roll and hangover.
2. **Features** (`MotionFeatures`). Per frame: user acceleration (3), rotation rate (3), and attitude change since the start of the segment (3). Each channel is mean-centered, and quiet lead-in and lead-out frames are trimmed.
3. **Matching** (`DTW`, `TemplateClassifier`). Each channel is z-scaled across the training set. Matching is 1-nearest-neighbour per word, using dynamic time warping with a Sakoe–Chiba band. A segment is rejected if the best distance is above *max distance* or the margin to the runner-up is below *min confidence*.
4. **Is it real?** (`Evaluation`). The leave-one-out accuracy is compared with the same procedure run on shuffled labels. If p < 0.05, the motion stream distinguishes your words better than chance.

## Experiment protocol

Use **Record Session** in the toolbar or on the Sensors screen. Recordings go to `~/Documents/Subvox/Sessions/<timestamp>-<condition>/`:

- `motion.csv`: every motion sample. `t` is seconds from session start.
- `audio.caf`: microphone audio, if the mic is running.
- `events.csv`: every Space-held segment, with its label or prediction.
- `meta.json`: condition, prompt, and the audio start offset for aligning audio with motion.

Suggested runs: the same 5-word script under **Normal speech**, **Whisper** and **Silent mouthing**, 10 reps each. Then, in Train, compare the Evaluate results across feature sets.

The training set lives at `~/Library/Application Support/Subvox/dataset.json`.

## Known limits

- Headphone motion is processed and low-rate (roughly 25 Hz on AirPods). The speech-detecting accelerometer and the inward mic are not exposed. Expect jaw and head movement, not vocal vibration.
- Templates are user- and session-specific. Re-seating the AirPods can shift the signal.
- Choosing any Bluetooth headset as the mic input switches it to the Bluetooth headset (HFP) profile.
- Aeropex is supported for audible whisper transcription only; it does not provide the motion stream used by Train and Decode.
