using System.Globalization;
using System.Text;
using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Infrastructure.Export;

/// <summary>
/// Exporte les données au format CSV.
/// </summary>
public sealed class CsvExportService : IExportService
{
    public async Task ExportCsvAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(assets);

        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        await writer.WriteLineAsync("AssetTag,Categorie,Fabricant,Modele,RamGo,SousEtat,Commentaire,NiveauAnomalie,MessageAnomalie");

        foreach (var asset in assets)
        {
            ct.ThrowIfCancellationRequested();

            var evaluation = asset.Evaluation;
            var values = new[]
            {
                Escape(asset.AssetTag),
                Escape(asset.Categorie.ToString()),
                Escape(asset.Fabricant),
                Escape(asset.Modele),
                Escape(asset.RamGo?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                Escape(asset.SousEtat.ToString()),
                Escape(asset.Commentaire),
                Escape(evaluation?.Niveau.ToString() ?? string.Empty),
                Escape(evaluation?.Message ?? string.Empty)
            };

            await writer.WriteLineAsync(string.Join(',', values));
        }

        await writer.FlushAsync();
    }

    public Task ExportXlsxAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
        => throw new NotSupportedException();

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }
}
