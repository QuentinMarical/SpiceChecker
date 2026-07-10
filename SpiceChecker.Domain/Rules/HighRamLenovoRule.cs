using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Règle prioritaire (consignes Enedis depuis mars 2026) sur les Lenovo 16/32 Go :
/// matériel stratégique (pénurie HP), jamais en Revalorisation ni en don.
/// Fonctionnel => Disponible Re-Use, défectueux => Réparation.
/// Les L14 G2 I7/R7 32 Go peuvent être conservés pour TMI.
/// </summary>
public sealed class HighRamLenovoRule : IRule
{
    public string Name => "HighRamLenovoRule";

    public bool IsOverride => true;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (!IsTargetAsset(asset))
        {
            return null;
        }

        var tmiSuffix = IsTmiModel(asset)
            ? " Modèle L14 G2 I7/R7 32 Go : à garder en stock pour TMI."
            : string.Empty;

        switch (asset.SousEtat)
        {
            case SousEtat.Revalorisation:
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = $"Lenovo {asset.RamGo} Go : ne doit jamais partir en Revalorisation (matériel stratégique, pénurie). Fonctionnel → Disponible Re-Use, défectueux → Réparation.{tmiSuffix}",
                    EstBloquant = true
                };

            case SousEtat.EnAttenteDeDon:
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = $"Lenovo {asset.RamGo} Go en attente de don : matériel stratégique à conserver, repasser en Disponible Re-Use.{tmiSuffix}",
                    EstBloquant = true
                };

            case SousEtat.Defectueux:
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Avertissement,
                    RegleDeclenchee = Name,
                    Message = $"Lenovo {asset.RamGo} Go défectueux : à renvoyer en Réparation (jamais en Revalorisation). Commentaire de panne obligatoire.{tmiSuffix}",
                    EstBloquant = true
                };

            case SousEtat.ABlanchir:
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Info,
                    RegleDeclenchee = Name,
                    Message = $"Lenovo {asset.RamGo} Go à blanchir : après blanchiment, conserver en Disponible Re-Use (matériel stratégique).{tmiSuffix}",
                    EstBloquant = true
                };

            default:
                // Conforme : le résultat non bloquant d'une règle override court-circuite les règles suivantes.
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Info,
                    RegleDeclenchee = Name,
                    Message = $"Lenovo {asset.RamGo} Go conforme : à conserver en Disponible Re-Use.{tmiSuffix}",
                    EstBloquant = false
                };
        }
    }

    private static bool IsTargetAsset(HardwareAsset asset)
    {
        var isComputer = asset.Categorie == CategorieEquipement.Ordinateur;
        var isLenovo = asset.Fabricant?.Contains("lenovo", StringComparison.OrdinalIgnoreCase) == true;
        var isTargetRam = asset.RamGo is 16 or 32;

        return isComputer && isLenovo && isTargetRam;
    }

    private static bool IsTmiModel(HardwareAsset asset)
    {
        var model = asset.Modele ?? string.Empty;
        return asset.RamGo == 32
            && model.Contains("L14 G2", StringComparison.OrdinalIgnoreCase)
            && (model.Contains("I7", StringComparison.OrdinalIgnoreCase)
                || model.Contains("R7", StringComparison.OrdinalIgnoreCase));
    }
}
