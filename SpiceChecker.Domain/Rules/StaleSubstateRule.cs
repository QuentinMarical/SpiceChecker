using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Détecte les sous-états transitoires inchangés depuis plus de 6 mois
/// (les stocks doivent être nettoyés régulièrement).
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

        var ageInDays = (int)(DateTime.Today - asset.DateDerniereModifSousEtat.Value.Date).TotalDays;
        var isTransientSubstate = asset.SousEtat is SousEtat.Revalorisation
            or SousEtat.RepriseEnAttente
            or SousEtat.ABlanchir
            or SousEtat.EnAttenteDeDon
            or SousEtat.Defectueux
            or SousEtat.EnReparation;

        if (ageInDays > StaleThresholdDays && isTransientSubstate)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Info,
                RegleDeclenchee = Name,
                Message = $"Sous-état « {asset.SousEtat.Libelle()} » inchangé depuis {ageInDays} jours (plus de 6 mois) : à vérifier.",
                EstBloquant = false
            };
        }

        return null;
    }
}
