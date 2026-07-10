using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Applique la règle prioritaire sur les Lenovo 16/32 Go :
/// fonctionnel => Disponible Re-Use, défectueux => Réparation, jamais Revalorisation.
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

        if (asset.SousEtat == SousEtat.Revalorisation)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = Name,
                Message = $"Lenovo {asset.RamGo} Go : ne doit jamais être en Revalorisation, matériel stratégique à conserver.",
                EstBloquant = true
            };
        }

        if (asset.SousEtat == SousEtat.Defectueux)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = Name,
                Message = $"Lenovo {asset.RamGo} Go défectueux : doit aller en Réparation, jamais en Revalorisation.",
                EstBloquant = true
            };
        }

        return new EvaluationResult
        {
            Niveau = NiveauAnomalie.Info,
            RegleDeclenchee = Name,
            Message = $"Lenovo {asset.RamGo} Go conforme : à conserver en Disponible Re-Use.",
            EstBloquant = false
        };
    }

    private static bool IsTargetAsset(HardwareAsset asset)
    {
        var isComputer = asset.Categorie == CategorieEquipement.Ordinateur;
        var isLenovo = asset.Fabricant?.Contains("lenovo", StringComparison.OrdinalIgnoreCase) == true;
        var isTargetRam = asset.RamGo is 16 or 32;

        return isComputer && isLenovo && isTargetRam;
    }
}