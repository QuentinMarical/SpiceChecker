using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Détecte les ordinateurs Lenovo avec 16 ou 32 Go de RAM à traiter en priorité.
/// </summary>
public sealed class HighRamLenovoRule : IRule
{
    /// <inheritdoc />
    public string Name => "HighRamLenovoRule";

    /// <inheritdoc />
    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var isComputer = asset.Categorie == CategorieEquipement.Ordinateur;
        var isLenovo = asset.Fabricant.Contains("lenovo", StringComparison.OrdinalIgnoreCase);
        var isTargetRam = asset.RamGo is 16 or 32;

        if (!isComputer || !isLenovo || !isTargetRam)
        {
            return null;
        }

        return new EvaluationResult
        {
            Niveau = NiveauAnomalie.Erreur,
            RegleDeclenchee = Name,
            Message = "Lenovo 16/32 Go détecté : à traiter en priorité (Override).",
            EstBloquant = true
        };
    }
}
