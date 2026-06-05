namespace SpiceChecker.Application.Services;

/// <summary>
/// Fournit l'accès aux paramètres applicatifs persistés.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Lit une valeur de paramètre, ou retourne la valeur par défaut.
    /// </summary>
    Task<string> GetSettingAsync(string key, string defaultValue);

    /// <summary>
    /// Sauvegarde une valeur de paramètre.
    /// </summary>
    Task SaveSettingAsync(string key, string value);
}
