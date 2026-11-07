using NAudio.Wave;
using Subvox.Windows.Models;

namespace Subvox.Windows.Services;

public static class AudioDeviceCatalog
{
    public static IReadOnlyList<AudioInputDevice> GetInputDevices()
    {
        var devices = new List<AudioInputDevice>();
        for (var index = 0; index < WaveIn.DeviceCount; index++)
        {
            var capabilities = WaveIn.GetCapabilities(index);
            devices.Add(new AudioInputDevice(index, capabilities.ProductName));
        }

        return devices;
    }
}
