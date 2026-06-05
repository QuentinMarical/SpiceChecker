using System.Text.Json;
using SpiceChecker.Application.Services;

namespace SpiceChecker.Infrastructure.Settings;

/// <summary>
/// Stockage des paramètres applicatifs dans %AppData%\SpiceChecker\settings.json.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _settingsFilePath;

    public JsonSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "SpiceChecker");
        _settingsFilePath = Path.Combine(folder, "settings.json");
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }

        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            var settings = await ReadSettingsAsync().ConfigureAwait(false);
            return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SaveSettingAsync(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            var settings = await ReadSettingsAsync().ConfigureAwait(false);
            settings[key] = value ?? string.Empty;

            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = new FileStream(_settingsFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions).ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<Dictionary<string, string>> ReadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            await using var stream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var settings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream).ConfigureAwait(false);
            return settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
