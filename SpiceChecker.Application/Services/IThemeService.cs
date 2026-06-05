namespace SpiceChecker.Application.Services;

/// <summary>
/// Fournit les thèmes disponibles et applique le thème sélectionné.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Retourne les thèmes disponibles.
    /// </summary>
    IReadOnlyList<string> GetAvailableThemes();

    /// <summary>
    /// Applique le thème fourni.
    /// </summary>
    /// <param name="themeName">Nom du thème.</param>
    void ApplyTheme(string themeName);

    /// <summary>
    /// Nom du thème courant.
    /// </summary>
    string CurrentTheme { get; }
}
