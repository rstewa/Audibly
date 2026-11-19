using System;
using System.Text.Json;
using Windows.Storage;
using Windows.UI;
using Audibly.ReadAlong;

namespace Audibly.App.Helpers;

public static class ReadAlongAppearanceSettings
{
    public static event EventHandler? Changed;
    public static ReadingPreferences Current { get; private set; } = Load();
    private static ReadingPreferences Load()
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values["ReadAlongAppearance"] is string json)
                return (JsonSerializer.Deserialize<ReadingPreferences>(json) ?? new()).Normalize();
        }
        catch (JsonException) { }
        var defaults = new ReadingPreferences();
        if (ApplicationData.Current.LocalSettings.Values["MiniReadAlongFontSize"] is double size)
            defaults = defaults with { MiniFontSize = size };
        return defaults.Normalize();
    }
    public static void Update(ReadingPreferences value)
    {
        Current = value.Normalize();
        ApplicationData.Current.LocalSettings.Values["ReadAlongAppearance"] = JsonSerializer.Serialize(Current);
        Changed?.Invoke(null, EventArgs.Empty);
    }
    public static Color Color(string value)
    {
        var rgb = Convert.ToUInt32(value[1..], 16);
        return Windows.UI.Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
