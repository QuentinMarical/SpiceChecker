using ClosedXML.Excel;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Infrastructure.Excel;

namespace SpiceChecker.Infrastructure.Tests;

/// <summary>
/// Tests de l'import Excel sur le format réel des exports SPICE / ServiceNow (alm_hardware).
/// </summary>
public class HardwareImportServiceTests
{
    private static readonly string[] SpiceHeaders =
    {
        "Etiquette", "État", "Sous-état", "Entrepôt", "Catégorie de modèle", "Modèle", "Date dernier sous état"
    };

    private static async Task<IReadOnlyList<HardwareAsset>> ImportAsync(params object[][] rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Page 1");

        for (var c = 0; c < SpiceHeaders.Length; c++)
        {
            ws.Cell(1, c + 1).Value = SpiceHeaders[c];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var value = rows[r][c];
                ws.Cell(r + 2, c + 1).Value = value switch
                {
                    DateTime date => date,
                    _ => XLCellValue.FromObject(value)
                };
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var service = new HardwareImportService();
        return await service.ImportAsync(stream);
    }

    private static object[] Row(string etiquette, string sousEtat, string modele, DateTime? dateSousEtat = null) =>
    [
        etiquette, "En stock", sousEtat, "OUE-ROUEN", "Ordinateur", modele, dateSousEtat ?? new DateTime(2026, 6, 1)
    ];

    [Fact]
    public async Task LaColonneDateDernierSousEtat_NecrasePasLeSousEtat()
    {
        // Bug historique : « Date dernier sous état » était mappée comme colonne Sous-état
        // et écrasait la vraie valeur, rendant tous les sous-états « Autre ».
        var assets = await ImportAsync(Row("SCR0000001", "Défectueux", "LENOVO THINKPAD_L14 G3 I5 16", new DateTime(2026, 6, 26)));

        var asset = Assert.Single(assets);
        Assert.Equal(SousEtat.Defectueux, asset.SousEtat);
        Assert.Equal(new DateTime(2026, 6, 26), asset.DateDerniereModifSousEtat);
    }

    [Theory]
    [InlineData("Disponible neuf", SousEtat.DisponibleNeuf)]
    [InlineData("Disponible Re-Use", SousEtat.Disponible)]
    [InlineData("Revalorisation (Déclassé, Retour loueur)", SousEtat.Revalorisation)]
    [InlineData("Réservé/Masterisé", SousEtat.ReserveMasterise)]
    [InlineData("A blanchir", SousEtat.ABlanchir)]
    [InlineData("En attente de don", SousEtat.EnAttenteDeDon)]
    [InlineData("Défectueux", SousEtat.Defectueux)]
    public async Task TousLesSousEtatsSpice_SontReconnus(string libelle, SousEtat attendu)
    {
        var assets = await ImportAsync(Row("SCR0000001", libelle, "LENOVO THINKPAD_L13 G1 I5 8"));

        var asset = Assert.Single(assets);
        Assert.Equal(attendu, asset.SousEtat);
    }

    [Theory]
    [InlineData("LENOVO THINKPAD_L13 G1 I5 8", "Lenovo", 8)]
    [InlineData("LENOVO THINKPAD_L14 G2 I7 32", "Lenovo", 32)]
    [InlineData("LENOVO THINKPAD_L14 G4 R5 16 256Go", "Lenovo", 16)]
    [InlineData("LENOVO THINKPAD_L14 G5 R5 16 512Go BIO", "Lenovo", 16)]
    [InlineData("HP ProBook 4G1a 14 R5 32Go 512Go", "HP", 32)]
    [InlineData("MICROSOFT SURFACE_PRO_7+ 8Go 256Go", "Microsoft", 8)]
    public async Task LaRam_EstExtraiteDuNomDeModele_SansConfondreLeStockage(string modele, string fabricantAttendu, int ramAttendue)
    {
        var assets = await ImportAsync(Row("SCR0000001", "Disponible Re-Use", modele));

        var asset = Assert.Single(assets);
        Assert.Equal(fabricantAttendu, asset.Fabricant);
        Assert.Equal(ramAttendue, asset.RamGo);
    }

    [Fact]
    public async Task UnEquipementReseau_SansRamDansLeNom_NaPasDeRam()
    {
        var assets = await ImportAsync(
        [
            "SG1BKN02HP", "En stock", "Disponible Re-Use", "OUE-ROUEN", "Équipement réseau", "Aruba 6300M 48G", new DateTime(2026, 4, 20)
        ]);

        var asset = Assert.Single(assets);
        Assert.Equal(CategorieEquipement.EquipementReseau, asset.Categorie);
        Assert.Null(asset.RamGo);
    }

    [Fact]
    public async Task EtatEtEntrepot_SontImportesSansPolluerLeCommentaire()
    {
        var assets = await ImportAsync(Row("SCR0000001", "Défectueux", "LENOVO THINKPAD_L13 G1 I5 8"));

        var asset = Assert.Single(assets);
        Assert.Equal("En stock", asset.Etat);
        Assert.Equal("OUE-ROUEN", asset.Entrepot);
        Assert.Equal(string.Empty, asset.Commentaire);
    }
}
