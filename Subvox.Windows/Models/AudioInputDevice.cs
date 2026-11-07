namespace Subvox.Windows.Models;

public sealed record AudioInputDevice(int DeviceNumber, string Name)
{
    public bool IsAeropex => AudioDevicePreference.IsPreferredName(Name);

    public string DisplayName => IsAeropex ? $"{Name}  ·  preferred" : Name;
}

public static class AudioDevicePreference
{
    private static readonly string[] PreferredTokens = ["aeropex", "aftershokz", "shokz"];

    public static bool IsPreferredName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        PreferredTokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase));

    public static AudioInputDevice? PickPreferred(IEnumerable<AudioInputDevice> devices)
    {
        var deviceList = devices.ToList();
        return deviceList.FirstOrDefault(device => device.IsAeropex) ?? deviceList.FirstOrDefault();
    }
}
