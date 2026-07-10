using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Détecte les sous-états sensibles inchangés depuis plus de 6 mois.
/// </summary>
public sealed class StaleSubstateRule : IRule
{
    private const int StaleThresholdDays = 183;

    public string Name => "StaleSubstateRule";

    public bool IsOverride => false;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (!asset.DateDerniereModifSousEtat.HasValue)
        {
            return null;
        }

        var ageInDays = (DateTime.Today - asset.DateDerniereModifSousEtat.Value.Date).TotalDays;
        var isEligibleSubstate = asset.SousEtat is SousEtat.RepriseEnAttente or SousEtat.Revalorisation;

        if (ageInDays > StaleThresholdDays && isEligibleSubstate)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Info,
                RegleDeclenchee = Name,
                Message = "Sous-état inchangé depuis plus de 6 mois, à vérifier.",
                EstBloquant = false
            };
        }

        return null;
    }
}