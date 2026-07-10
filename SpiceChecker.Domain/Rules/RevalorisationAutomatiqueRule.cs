using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Revalorisation automatique, sans contrainte de date de renouvellement :
/// - Modèles anciens (L390 ou antérieurs : L390, L450, L460, L470, L480, T580...)
/// - Surface Pro défectueux (non pris en charge par le service réparation Enedis)
/// - Ordinateurs non Lenovo / non HP défectueux
/// Note : les Lenovo 16/32 Go sont exclus en amont par la règle prioritaire HighRamLenovoRule.
/// </summary>
public sealed class RevalorisationAutomatiqueRule : IRule
{
    private static readonly string[] ObsoleteModelMarkers =
    {
        "L390",
        "L380",
        "L450",
        "L460",
        "L470",
        "L480",
        "T460",
        "T470",
        "T480",
        "T570",
        "T580"
    };

    public string Name => "RevalorisationAutomatiqueRule";

    public bool IsOverride => false;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.Categorie != CategorieEquipement.Ordinateur)
        {
            return null;
        }

        // Sous-états de sortie de parc déjà engagés : rien à signaler ici.
        if (asset.SousEtat is SousEtat.Revalorisation or SousEtat.EnAttenteDeDon or SousEtat.RepriseEnAttente)
        {
            return null;
        }

        var model = asset.Modele ?? string.Empty;
        var fabricant = asset.Fabricant ?? string.Empty;

        if (IsObsoleteModel(model))
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = $"{model} : modèle ancien (L390 ou antérieur) → Revalorisation automatique, peu importe la date de renouvellement.",
                EstBloquant = false
            };
        }

        if (asset.SousEtat != SousEtat.Defectueux)
        {
            return null;
        }

        if (IsSurfacePro(model, fabricant))
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = "Surface Pro défectueux (non pris en charge par le service réparation Enedis) : à passer en Revalorisation avec commentaire de panne.",
                EstBloquant = false
            };
        }

        var isLenovoOrHp = fabricant.Contains("lenovo", StringComparison.OrdinalIgnoreCase)
                        || fabricant.Contains("hp", StringComparison.OrdinalIgnoreCase);

        if (!isLenovoOrHp)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = $"Ordinateur {fabricant} défectueux (hors Lenovo/HP, non réparable) : à passer en Revalorisation avec commentaire de panne.",
                EstBloquant = false
            };
        }

        return null;
    }

    private static bool IsObsoleteModel(string model)
    {
        return ObsoleteModelMarkers.Any(marker => model.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSurfacePro(string model, string fabricant)
    {
        return model.Contains("SURFACE", StringComparison.OrdinalIgnoreCase)
            || (fabricant.Contains("microsoft", StringComparison.OrdinalIgnoreCase)
                && model.Contains("PRO", StringComparison.OrdinalIgnoreCase));
    }
}
