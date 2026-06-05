using ClosedXML.Excel;
using FluentAssertions;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Infrastructure.Excel;

namespace SpiceChecker.Infrastructure.Tests.Excel;

public sealed class HardwareImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ParsesAllColumnsCorrectly_IncludingDatesAndRam()
    {
        using var stream = CreateTestExcelStream(includeDateRenouvColumn: true);
        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(3);

        result[0].AssetTag.Should().Be("ASSET-001");
        result[0].Categorie.Should().Be(CategorieEquipement.Ordinateur);
        result[0].SousEtat.Should().Be(SousEtat.Disponible);
        result[0].RamGo.Should().Be(8);
        result[0].DateRenouvellement.Should().Be(new DateTime(2026, 6, 1));

        result[1].Fabricant.Should().Be("LENOVO");
        result[1].RamGo.Should().Be(32);
        result[1].DateRenouvellement.Should().Be(new DateTime(2024, 12, 31));

        result[2].SousEtat.Should().Be(SousEtat.Defectueux);
        result[2].DateAcquisition.Should().Be(new DateTime(2023, 10, 5));
        result[2].DateDerniereModifSousEtat.Should().Be(new DateTime(2025, 2, 12));
    }

    [Fact]
    public async Task ImportAsync_HandlesMissingOptionalColumnsGracefully()
    {
        using var stream = CreateTestExcelStream(includeDateRenouvColumn: false);
        var service = new HardwareImportService();

        var result = await service.ImportAsync(stream);

        result.Should().HaveCount(3);
        result.Should().OnlyContain(a => a.DateRenouvellement == null);
    }

    private static MemoryStream CreateTestExcelStream(bool includeDateRenouvColumn)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Import");

        var headers = new List<string>
        {
            "N° Asset",
            "Catégorie",
            "Fab",
            "Modèle",
            "RAM",
            "Date Acquisition"
        };

        if (includeDateRenouvColumn)
        {
            headers.Add("Date Renouv.");
        }

        headers.AddRange(new[] { "Sous État", "Commentaire", "Date Modif" });

        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        WriteRow(ws, 2, includeDateRenouvColumn, "ASSET-001", "Ordinateur", "Dell", "Latitude 5420", "8GB", "01/01/2022", "01/06/2026", "Disponible", "OK", "10/01/2026");
        WriteRow(ws, 3, includeDateRenouvColumn, "ASSET-002", "Ordinateur", "LENOVO", "ThinkPad L14", "32 Go", "15/03/2021", "31/12/2024", "Revalorisation", "Poste ancien", "05/02/2025");
        WriteRow(ws, 4, includeDateRenouvColumn, "ASSET-003", "Périphérique", "HP", "Dock", "", "05/10/2023", "12/11/2027", "Défectueux", "HS alimentation", "12/02/2025");

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        workbook.Dispose();
        return stream;
    }

    private static void WriteRow(
        IXLWorksheet ws,
        int row,
        bool includeDateRenouvColumn,
        string asset,
        string categorie,
        string fabricant,
        string modele,
        string ram,
        string dateAcquisition,
        string dateRenouvellement,
        string sousEtat,
        string commentaire,
        string dateModif)
    {
        var values = new List<string>
        {
            asset,
            categorie,
            fabricant,
            modele,
            ram,
            dateAcquisition
        };

        if (includeDateRenouvColumn)
        {
            values.Add(dateRenouvellement);
        }

        values.AddRange(new[] { sousEtat, commentaire, dateModif });

        for (var i = 0; i < values.Count; i++)
        {
            ws.Cell(row, i + 1).Value = values[i];
        }
    }
}
