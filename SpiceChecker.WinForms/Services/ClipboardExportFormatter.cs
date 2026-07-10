using System.Text;
using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Formateur de texte tabulé pour la copie d'équipements.
/// </summary>
public sealed class ClipboardExportFormatter : IClipboardExportFormatter
{
    /// <inheritdoc />
    public string FormatAssets(IReadOnlyList<HardwareAsset> assets)
    {
        if (assets.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Étiquette\tFabricant\tModèle\tSous-état\tNiveau\tRésultat d'analyse");

        foreach (var asset in assets)
        {
            var niveau = asset.Evaluation?.Niveau.ToString() ?? string.Empty;
            var anomaly = asset.Evaluation?.Message ?? string.Empty;
            sb.Append(asset.AssetTag).Append('\t')
              .Append(asset.Fabricant).Append('\t')
              .Append(asset.Modele).Append('\t')
              .Append(asset.SousEtat.Libelle()).Append('\t')
              .Append(niveau).Append('\t')
              .AppendLine(anomaly);
        }

        return sb.ToString();
    }
}
