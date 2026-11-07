# Windows whisper-mode UI brief

## User and job

The primary user wears an Aeropex headset on Windows and wants a fast way to turn quiet, audible speech into text. The first-use path is: confirm the Aeropex headset microphone, download the local speech model once, record a short passage, then review or copy the transcript.

## Reference patterns

- Windows Voice Access keeps the current listening state explicit and makes microphone setup visible.
- Windows Voice Typing uses a compact, single-purpose dictation flow with an obvious start/stop control.
- Buzz demonstrates that local Whisper tools need clear model, input, progress, and transcription states.

These patterns are adapted into one focused window rather than a job queue or a system-wide accessibility toolbar.

## Visual direction

- Native Windows desktop surface using Segoe UI and keyboard navigation.
- Dark navy neutral background, one teal accent, and a red recording state.
- 8/12/16/20 px spacing rhythm, 8–10 px corner radii, and two functional panels.
- Moderate title scale; status and task controls take priority over branding.

## States and edge cases

- No microphone: explain how to connect Aeropex and offer refresh.
- Non-Aeropex microphone: remain usable while explaining how to switch.
- Model missing/downloading/ready/failed: keep recording disabled until ready.
- Ready/listening/transcribing/success/no speech/error: use persistent text plus a color dot, never color alone.
- Empty transcript: retain the task explanation and privacy statement.
- Temporary recordings are removed after transcription; only the downloaded model persists.

## Hardware truth

Aeropex uses bone conduction for playback, but Windows receives voice through its conventional Bluetooth headset microphone using HFP. The app improves the workflow and local recognition; it does not claim access to jaw vibration or a hidden bone-conduction input sensor.
