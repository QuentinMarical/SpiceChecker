namespace SpiceChecker.Application.Services;

/// <summary>
/// Fournit un accès au presse-papier système.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copie un texte dans le presse-papier.
    /// </summary>
    /// <param name="text">Texte à copier.</param>
    void SetText(string text);
}
