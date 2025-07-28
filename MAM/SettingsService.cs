using System.Text.Json;
using System.IO;
using System.Diagnostics;

public static class SettingsService
{
    private static readonly string SettingsFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MAM"); // Your app folder name

    private static readonly string SettingsFilePath = Path.Combine(SettingsFolderPath, "settings.json");

    private static Dictionary<string, object> _settings = new();

   
    static SettingsService()
    {
        try
        {
            Load();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to load settings: " + ex.Message);
            _settings = new Dictionary<string, object>();
        }
    }

    public static void Save()
    {
        if (!Directory.Exists(SettingsFolderPath))
        {
            Directory.CreateDirectory(SettingsFolderPath);
        }

        var json = JsonSerializer.Serialize(_settings);
        File.WriteAllText(SettingsFilePath, json);
    }
    private static void Load()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _settings = new Dictionary<string, object>();
                    return;
                }

                _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                            ?? new Dictionary<string, object>();
            }
            catch (JsonException)
            {
                // Corrupted file: back it up or delete
                File.Move(SettingsFilePath, SettingsFilePath + ".bak", overwrite: true);
                _settings = new Dictionary<string, object>();
            }
        }
        else
        {
            _settings = new Dictionary<string, object>();
        }
    }

    //private static void Load()
    //{
    //    if (File.Exists(SettingsFilePath))
    //    {
    //        var json = File.ReadAllText(SettingsFilePath);
    //        _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
    //    }
    //}

    public static void Set(string key, object value)
    {
        _settings[key] = value;
        Save();
    }

    public static T Get<T>(string key, T defaultValue = default)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)element.GetString();
                    if (typeof(T) == typeof(bool))
                        return (T)(object)element.GetBoolean();
                    if (typeof(T) == typeof(int))
                        return (T)(object)element.GetInt32();
                    if (typeof(T) == typeof(double))
                        return (T)(object)element.GetDouble();
                    // Add more types if needed
                }
                else
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public static void Remove(string key)
    {
        if (_settings.Remove(key))
        {
            Save();
        }
    }
}

