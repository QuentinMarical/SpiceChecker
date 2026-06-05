using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Tests.Helpers;

internal static class HardwareAssetTestHelper
{
    public static HardwareAsset CreateAsset(
        string fabricant = "Lenovo",
        int? ramGo = 16,
        CategorieEquipement categorie = CategorieEquipement.Ordinateur,
        SousEtat sousEtat = SousEtat.Disponible,
        string commentaire = "",
        string modele = "ThinkPad T14",
        DateTime? dateRenouvellement = null,
        DateTime? dateDerniereModifSousEtat = null)
    {
        return new HardwareAsset
        {
            AssetTag = "ASSET-001",
            Categorie = categorie,
            Fabricant = fabricant,
            Modele = modele,
            RamGo = ramGo,
            DateRenouvellement = dateRenouvellement,
            DateDerniereModifSousEtat = dateDerniereModifSousEtat,
            SousEtat = sousEtat,
            Commentaire = commentaire
        };
    }
}
