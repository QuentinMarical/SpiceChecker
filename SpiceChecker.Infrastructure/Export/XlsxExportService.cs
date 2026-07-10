using ClosedXML.Excel;
using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Infrastructure.Export;

/// <summary>
/// Exporte les données au format Excel.
/// </summary>
public sealed class XlsxExportService : IExportService
{
    public Task ExportCsvAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
        => throw new NotSupportedException();

    public async Task ExportXlsxAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(assets);

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("SpiceChecker Export");

            var headers = new[]
            {
                "Étiquette", "Catégorie", "Fabricant", "Modèle", "RAM (Go)", "Sous-état", "Entrepôt", "Renouvellement", "Commentaire", "Niveau", "Résultat d'analyse"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x4E, 0x79);
            headerRange.Style.Font.FontColor = XLColor.White;

            for (var row = 0; row < assets.Count; row++)
            {
                ct.ThrowIfCancellationRequested();

                var asset = assets[row];
                var evaluation = asset.Evaluation;
                var excelRow = row + 2;

                worksheet.Cell(excelRow, 1).Value = asset.AssetTag;
                worksheet.Cell(excelRow, 2).Value = asset.Categorie.Libelle();
                worksheet.Cell(excelRow, 3).Value = asset.Fabricant;
                worksheet.Cell(excelRow, 4).Value = asset.Modele;
                worksheet.Cell(excelRow, 5).Value = asset.RamGo;
                worksheet.Cell(excelRow, 6).Value = asset.SousEtat.Libelle();
                worksheet.Cell(excelRow, 7).Value = asset.Entrepot;
                worksheet.Cell(excelRow, 8).Value = asset.DateRenouvellement?.ToString("dd/MM/yyyy") ?? string.Empty;
                worksheet.Cell(excelRow, 9).Value = asset.Commentaire;
                worksheet.Cell(excelRow, 10).Value = evaluation?.Niveau.ToString() ?? string.Empty;
                worksheet.Cell(excelRow, 11).Value = evaluation?.Message ?? string.Empty;

                ApplySeverityColor(worksheet.Cell(excelRow, 10), evaluation?.Niveau);
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }, ct);
    }

    private static void ApplySeverityColor(IXLCell cell, NiveauAnomalie? niveau)
    {
        cell.Style.Fill.BackgroundColor = niveau switch
        {
            NiveauAnomalie.Bloquant or NiveauAnomalie.Erreur => XLColor.Red,
            NiveauAnomalie.Avertissement => XLColor.Orange,
            NiveauAnomalie.Info => XLColor.LightBlue,
            _ => XLColor.NoColor
        };
    }
}
