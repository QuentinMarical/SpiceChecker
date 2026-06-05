using System.Text;
using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;

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
        sb.AppendLine("AssetTag\tFabricant\tModele\tSousEtat\tAnomalie");

        foreach (var asset in assets)
        {
            var anomaly = asset.Evaluation?.Message ?? string.Empty;
            sb.Append(asset.AssetTag).Append('\t')
              .Append(asset.Fabricant).Append('\t')
              .Append(asset.Modele).Append('\t')
              .Append(asset.SousEtat).Append('\t')
              .AppendLine(anomaly);
        }

        return sb.ToString();
    }
}
