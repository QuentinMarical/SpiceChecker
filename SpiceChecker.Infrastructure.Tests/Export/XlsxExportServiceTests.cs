using ClosedXML.Excel;
using FluentAssertions;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Infrastructure.Export;

namespace SpiceChecker.Infrastructure.Tests.Export;

public sealed class XlsxExportServiceTests
{
    private static HardwareAsset BuildAsset(string assetTag, EvaluationResult? evaluation) => new()
    {
        AssetTag = assetTag,
        Categorie = CategorieEquipement.Ordinateur,
        Fabricant = "Dell",
        Modele = "Latitude 5420",
        RamGo = 16,
        SousEtat = SousEtat.Disponible,
        Commentaire = "Test",
        Evaluation = evaluation
    };

    [Fact]
    public async Task ExportXlsxAsync_WritesExpectedSheetHeadersAndSeverityColor()
    {
        // Arrange
        var assets = new[]
        {
            BuildAsset("TAG001", new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = "Rule1",
                Message = "Erreur critique",
                EstBloquant = true
            }),
            BuildAsset("TAG002", null)
        };

        await using var stream = new MemoryStream();
        var service = new XlsxExportService();

        // Act
        await service.ExportXlsxAsync(stream, assets, CancellationToken.None);
        stream.Position = 0;

        // Assert
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("SpiceChecker Export");

        var headers = worksheet.Row(1).Cells(1, 9).Select(c => c.GetString()).ToArray();
        headers.Should().Equal(
            "AssetTag", "Categorie", "Fabricant", "Modele", "RamGo", "SousEtat", "Commentaire", "NiveauAnomalie", "MessageAnomalie");

        worksheet.LastRowUsed()!.RowNumber().Should().Be(3);
        worksheet.Cell(2, 8).GetString().Should().Be(NiveauAnomalie.Erreur.ToString());
        worksheet.Cell(2, 8).Style.Fill.BackgroundColor.Color.Should().Be(XLColor.Red.Color);
        worksheet.Cell(3, 8).GetString().Should().BeEmpty();
    }
}
