using ClosedXML.Excel;
using FluentAssertions;
using SpiceChecker.Infrastructure.Excel;

namespace SpiceChecker.Infrastructure.Tests.Excel;

public sealed class HardwareImportServiceModelNormalizationTests
{
    [Fact]
    public async Task ImportAsync_ExtractsManufacturerFromModele_AndRemovesPrefixFromModele()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");

        ws.Cell(1, 1).Value = "Étiquette";
        ws.Cell(1, 2).Value = "Catégorie de modèle";
        ws.Cell(1, 3).Value = "Modèle";
        ws.Cell(2, 1).Value = "SCR1013388";
        ws.Cell(2, 2).Value = "Ordinateur";
        ws.Cell(2, 3).Value = "HP ProBook 450 G8";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(1);
        result[0].Fabricant.Should().Be("HP");
        result[0].Modele.Should().Be("ProBook 450 G8");
    }

    [Fact]
    public async Task ImportAsync_ExtractsRamFromModele_AndRemovesTrailingRamSuffix()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");

        ws.Cell(1, 1).Value = "Étiquette";
        ws.Cell(1, 2).Value = "Catégorie de modèle";
        ws.Cell(1, 3).Value = "Modèle";
        ws.Cell(2, 1).Value = "SCR1013388";
        ws.Cell(2, 2).Value = "Ordinateur";
        ws.Cell(2, 3).Value = "HP ProBook 4G1a 14 32Go 512Go";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(1);
        result[0].Fabricant.Should().Be("HP");
        result[0].Modele.Should().Be("ProBook 4G1a 14 32Go 512Go");
        result[0].RamGo.Should().Be(32);
    }

    [Fact]
    public async Task ImportAsync_DoesNotUseCpuAdjacentNumberAsRam_WhenStorageFollowsImmediately()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");

        ws.Cell(1, 1).Value = "Étiquette";
        ws.Cell(1, 2).Value = "Catégorie de modèle";
        ws.Cell(1, 3).Value = "Modèle";
        ws.Cell(2, 1).Value = "SCR0115310";
        ws.Cell(2, 2).Value = "Ordinateur";
        ws.Cell(2, 3).Value = "LENOVO THINKPAD_L14 G4 R5 16 256Go";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(1);
        result[0].Fabricant.Should().Be("Lenovo");
        result[0].Modele.Should().Be("THINKPAD_L14 G4 R5 16");
        result[0].RamGo.Should().BeNull();
    }

    [Fact]
    public async Task ImportAsync_DoesNotConfuseStorageWithRam_WhenOnlyStorageIsPresent()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");

        ws.Cell(1, 1).Value = "Étiquette";
        ws.Cell(1, 2).Value = "Catégorie de modèle";
        ws.Cell(1, 3).Value = "Modèle";
        ws.Cell(2, 1).Value = "SRV-001";
        ws.Cell(2, 2).Value = "Serveur";
        ws.Cell(2, 3).Value = "Dell PowerEdge R740 512Go SSD";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(1);
        result[0].Fabricant.Should().Be("Dell");
        result[0].Modele.Should().Be("PowerEdge R740 512Go SSD");
        result[0].RamGo.Should().BeNull();
    }

    [Fact]
    public async Task ImportAsync_MapsEtatAndLastUpdateColumns()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");

        ws.Cell(1, 1).Value = "Étiquette";
        ws.Cell(1, 2).Value = "Catégorie de modèle";
        ws.Cell(1, 3).Value = "Fabricant";
        ws.Cell(1, 4).Value = "Modèle";
        ws.Cell(1, 5).Value = "État";
        ws.Cell(1, 6).Value = "Last Update";
        ws.Cell(2, 1).Value = "SCR7000001";
        ws.Cell(2, 2).Value = "Ordinateur";
        ws.Cell(2, 3).Value = "Lenovo";
        ws.Cell(2, 4).Value = "ThinkPad L14";
        ws.Cell(2, 5).Value = "Défectueux";
        ws.Cell(2, 6).Value = "10/06/2025";

        ws.Cell(3, 1).Value = "SCR0089426";
        ws.Cell(3, 2).Value = "Ordinateur";
        ws.Cell(3, 3).Value = string.Empty;
        ws.Cell(3, 4).Value = "MICROSOFT SURFACE_PRO_7+ 8Go 256Go";
        ws.Cell(3, 5).Value = "En stock";
        ws.Cell(3, 6).Value = "15/06/2025";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(2);
        result[0].SousEtat.Should().Be(SpiceChecker.Domain.Enums.SousEtat.Defectueux);
        result[0].DateDerniereModifSousEtat.Should().Be(new DateTime(2025, 6, 10));

        result[1].Fabricant.Should().Be("Microsoft");
        result[1].Modele.Should().Be("SURFACE_PRO_7+ 8Go 256Go");
        result[1].SousEtat.Should().Be(SpiceChecker.Domain.Enums.SousEtat.Disponible);
    }
}
