using System.Text.Json;

namespace JustSpell.Core;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Current { get; private set; }

    public event Action<AppSettings>? Changed;

    public SettingsStore(AppSettings current)
    {
        Current = current;
        Current.Normalize();
    }

    public static SettingsStore Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsFile))
            {
                var json = File.ReadAllText(AppPaths.SettingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                loaded.Normalize();
                return new SettingsStore(loaded);
            }
        }
        catch
        {
            // Fall through to defaults. A corrupt settings file should never prevent launch.
        }

        return new SettingsStore(new AppSettings());
    }

    public void Save()
    {
        Current.Normalize();
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(AppPaths.SettingsFile, json);
        Changed?.Invoke(Current);
    }
}
