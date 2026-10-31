using System.Text.Json;

namespace POS.Desktop.Services;

public class DesktopFileStorageService
{
    private readonly string _settingsFilePath;
    private Dictionary<string, string> _settings;

    public DesktopFileStorageService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "POSDesktop"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
        _settings = LoadSettings();
    }

    private Dictionary<string, string> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                    ?? new Dictionary<string, string>();
            }
        }
        catch (Exception)
        {
            // If file is corrupted or can't be read, return empty dictionary
        }
        return new Dictionary<string, string>();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception)
        {
            // Log error if needed
        }
    }

    public Task<T?> GetItemAsync<T>(string key)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(value);
                return Task.FromResult(result);
            }
            catch
            {
                return Task.FromResult<T?>(default);
            }
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            _settings[key] = json;
            SaveSettings();
        }
        catch
        {
            // Log error if needed
        }
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        _settings.Remove(key);
        SaveSettings();
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _settings.Clear();
        SaveSettings();
        return Task.CompletedTask;
    }

    public bool ContainsKey(string key) => _settings.ContainsKey(key);

    public int Count => _settings.Count;

    public IEnumerable<string> GetKeys() => _settings.Keys.ToList();

    public string? KeyAt(int index)
    {
        if (index < 0 || index >= _settings.Count) return null;
        return _settings.Keys.ElementAt(index);
    }
}
