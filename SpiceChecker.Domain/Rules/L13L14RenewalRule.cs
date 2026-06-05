using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Détecte les Lenovo L13/L14 8Go dont le renouvellement est échu ou imminent.
/// </summary>
public sealed class L13L14RenewalRule : IRule
{
    /// <inheritdoc />
    public string Name => "L13L14RenewalRule";

    /// <inheritdoc />
    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var isComputer = asset.Categorie == CategorieEquipement.Ordinateur;
        var isLenovo = asset.Fabricant.Contains("lenovo", StringComparison.OrdinalIgnoreCase);
        var isL13OrL14 = asset.Modele.Contains("L13", StringComparison.OrdinalIgnoreCase)
                         || asset.Modele.Contains("L14", StringComparison.OrdinalIgnoreCase);
        var is8Go = asset.RamGo == 8;
        var isRenewalDueOrImminent = asset.DateRenouvellement.HasValue
                                     && asset.DateRenouvellement.Value.Date <= DateTime.Today;

        if (!isComputer || !isLenovo || !isL13OrL14 || !is8Go || !isRenewalDueOrImminent)
        {
            return null;
        }

        return new EvaluationResult
        {
            Niveau = NiveauAnomalie.Avertissement,
            RegleDeclenchee = Name,
            Message = "Modèle Lenovo L13/L14 8Go dont le renouvellement est échu ou imminent.",
            EstBloquant = false
        };
    }
}
