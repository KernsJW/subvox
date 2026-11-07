import AppKit
import CoreAudio
import SwiftUI

/// Quiet speech-to-text using any Mac input, with first-class Aeropex selection.
struct WhisperView: View {
    @Environment(AppModel.self) private var model

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                introduction
                microphone
                transcript
            }
            .padding(20)
        }
    }

    private var introduction: some View {
        GroupBox {
            HStack(alignment: .top, spacing: 14) {
                Image(systemName: "waveform.and.mic")
                    .font(.system(size: 36))
                    .foregroundStyle(.tint)
                VStack(alignment: .leading, spacing: 6) {
                    Text("Aeropex whisper dictation")
                        .font(.title2.weight(.semibold))
                    Text("Whisper normally and Subvox transcribes the Aeropex microphone. This mode does not need headphone-motion access or a trained vocabulary.")
                    Text("Aeropex uses bone conduction for playback, but exposes a conventional Bluetooth headset microphone—not jaw-vibration or head-motion sensor data.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .padding(6)
        }
    }

    private var microphone: some View {
        @Bindable var audio = model.audio
        let preferred = AudioDevices.preferredWhisperInput(in: audio.devices)
        let selected = audio.devices.first { $0.id == audio.selectedDeviceID }

        return GroupBox("Microphone") {
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    Picker("Input", selection: $audio.selectedDeviceID) {
                        Text("System default").tag(AudioDeviceID?.none)
                        ForEach(audio.devices) { device in
                            Text(device.name).tag(Optional(device.id))
                        }
                    }
                    Button("Refresh", systemImage: "arrow.clockwise") { audio.refreshDevices() }
                        .labelStyle(.iconOnly)
                    if let preferred, preferred.id != audio.selectedDeviceID {
                        Button("Use \(preferred.name)") { audio.selectedDeviceID = preferred.id }
                    }
                }

                HStack(spacing: 12) {
                    Button(audio.isRunning ? "Stop Whispering" : "Start Whispering") {
                        Task { await model.toggleMic() }
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(audio.isRunning ? .red : .accentColor)

                    LevelMeter(value: Double(audio.level))
                    Text(audio.isRunning ? "Listening" : "Stopped")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                if let name = audio.activeDeviceName ?? selected?.name {
                    Label(
                        audio.isRunning ? "Capturing from \(name)" : "Ready to use \(name)",
                        systemImage: AudioDevices.isWhisperHeadsetName(name) ? "headphones" : "mic")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                } else if preferred == nil {
                    Text("Pair Aeropex in macOS Bluetooth settings, then choose Refresh. You can also use the Mac's built-in microphone.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                if let error = audio.error ?? model.transcriber.error {
                    Text(error).font(.caption).foregroundStyle(.red)
                }
                Text("Bluetooth microphone use switches Aeropex to the call-quality HFP profile. Keep its microphone openings clear and move to a quieter room if whispers do not register.")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .padding(6)
        }
    }

    private var transcript: some View {
        let transcriber = model.transcriber
        return GroupBox("Transcript") {
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    if transcriber.isRunning {
                        Label(
                            transcriber.isOnDevice ? "On-device recognition" : "Server recognition",
                            systemImage: transcriber.isOnDevice ? "laptopcomputer" : "network")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    Spacer()
                    Button("Copy") {
                        NSPasteboard.general.clearContents()
                        NSPasteboard.general.setString(transcriber.text, forType: .string)
                    }
                    .disabled(transcriber.text.isEmpty)
                    Button("Clear", action: transcriber.clear)
                        .disabled(transcriber.text.isEmpty)
                }

                Text(transcriber.text.isEmpty ? "Your whispered words will appear here…" : transcriber.text)
                    .font(.system(size: 26, weight: .regular, design: .rounded))
                    .foregroundStyle(transcriber.text.isEmpty ? .tertiary : .primary)
                    .textSelection(.enabled)
                    .frame(maxWidth: .infinity, minHeight: 220, alignment: .topLeading)
            }
            .padding(6)
        }
    }
}
