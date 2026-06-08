using ClosedXML.Excel;
using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Infrastructure.Excel;

namespace SpiceChecker.Infrastructure.Tests.Excel;

public sealed class HardwareImportServiceSpiceHeadersTests
{
    [Fact]
    public async Task ImportAsync_ParsesSpiceStyleHeaders_WhenUsingEtiquetteAndCategorieDeModele()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("SPICE");

        var headers = new[]
        {
            "Étiquette",
            "Catégorie de modèle",
            "Fabricant",
            "Modèle",
            "RAM",
            "Date renouvellement matériel",
            "Date changement sous-état",
            "Sous-état",
            "Commentaire"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(3, i + 1).Value = headers[i];
        }

        ws.Cell(4, 1).Value = "SPICE-001";
        ws.Cell(4, 2).Value = "Ordinateur";
        ws.Cell(4, 3).Value = "Dell";
        ws.Cell(4, 4).Value = "Latitude 7420";
        ws.Cell(4, 5).Value = "16 Go";
        ws.Cell(4, 6).Value = "01/06/2027";
        ws.Cell(4, 7).Value = "15/02/2025";
        ws.Cell(4, 8).Value = "Disponible";
        ws.Cell(4, 9).Value = "RAS";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(1);
        result[0].AssetTag.Should().Be("SPICE-001");
        result[0].Categorie.Should().Be(CategorieEquipement.Ordinateur);
        result[0].Fabricant.Should().Be("Dell");
        result[0].Modele.Should().Be("Latitude 7420");
        result[0].RamGo.Should().Be(16);
        result[0].DateRenouvellement.Should().Be(new DateTime(2027, 6, 1));
        result[0].SousEtat.Should().Be(SousEtat.Disponible);
        result[0].DateDerniereModifSousEtat.Should().Be(new DateTime(2025, 2, 15));
    }
}
