using ClosedXML.Excel;
using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Infrastructure.Excel;

namespace SpiceChecker.Infrastructure.Tests.Excel;

public sealed class HardwareImportServiceRealSampleShapeTests
{
    [Fact]
    public async Task ImportAsync_ParsesRealSampleShape_WithEtatSousEtatEntrepotAndLastDate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Page 1");

        ws.Cell(1, 1).Value = "Etiquette";
        ws.Cell(1, 2).Value = "État";
        ws.Cell(1, 3).Value = "Sous-état";
        ws.Cell(1, 4).Value = "Entrepôt";
        ws.Cell(1, 5).Value = "Catégorie de modèle";
        ws.Cell(1, 6).Value = "Modèle";
        ws.Cell(1, 7).Value = "Date dernier sous état";

        ws.Cell(2, 1).Value = "SG1BKN02HP";
        ws.Cell(2, 2).Value = "En stock";
        ws.Cell(2, 3).Value = "Disponible Re-Use";
        ws.Cell(2, 4).Value = "OUE-ROUEN";
        ws.Cell(2, 5).Value = "Équipement réseau";
        ws.Cell(2, 6).Value = "Aruba 6300M 48G";
        ws.Cell(2, 7).Value = new DateTime(2026, 4, 20);

        ws.Cell(3, 1).Value = "SCR0108557";
        ws.Cell(3, 2).Value = "En stock";
        ws.Cell(3, 3).Value = "Réservé/Masterisé";
        ws.Cell(3, 4).Value = "OUE-ROUEN";
        ws.Cell(3, 5).Value = "Ordinateur";
        ws.Cell(3, 6).Value = "LENOVO THINKPAD_L14 BOOSTE G4 R7 32 512Go";
        ws.Cell(3, 7).Value = new DateTime(2026, 5, 6);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();
        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(2);
        result[0].AssetTag.Should().Be("SG1BKN02HP");
        result[0].Categorie.Should().Be(CategorieEquipement.EquipementReseau);
        result[0].Fabricant.Should().Be("Aruba");
        result[0].Modele.Should().Be("6300M 48G");
        result[0].RamGo.Should().Be(48);
        result[0].SousEtat.Should().Be(SousEtat.Disponible);
        result[0].Commentaire.Should().Contain("État: En stock");
        result[0].Commentaire.Should().Contain("Entrepôt: OUE-ROUEN");
        result[0].DateDerniereModifSousEtat.Should().Be(new DateTime(2026, 4, 20));

        result[1].AssetTag.Should().Be("SCR0108557");
        result[1].Fabricant.Should().Be("Lenovo");
        result[1].Modele.Should().Be("THINKPAD_L14 BOOSTE G4 R7 32 512Go");
        result[1].SousEtat.Should().Be(SousEtat.RepriseEnAttente);
        result[1].DateDerniereModifSousEtat.Should().Be(new DateTime(2026, 5, 6));
    }
}
