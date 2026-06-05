using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.Services;

/// <summary>
/// Formate une liste d'équipements pour une copie texte dans le presse-papier.
/// </summary>
public interface IClipboardExportFormatter
{
    /// <summary>
    /// Formate les équipements sous forme de texte tabulé.
    /// </summary>
    /// <param name="assets">Équipements à formater.</param>
    string FormatAssets(IReadOnlyList<HardwareAsset> assets);
}
