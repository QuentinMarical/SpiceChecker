using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Applique les règles métier Lenovo L13/L14 8 Go :
/// - L13 G1 défectueux => Revalorisation obligatoire
/// - L13 G2 / L14 défectueux => Revalorisation si renouvellement <= 2027, sinon Réparation
/// - Fonctionnels => Re-Use
/// </summary>
public sealed class L13L14RenewalRule : IRule
{
    private static readonly string[] L13G1Markers =
    {
        "g1",
        "gen 1",
        "21rb",
        "21re",
        "21rc",
        "21rd"
    };

    public string Name => "L13L14RenewalRule";

    public bool IsOverride => false;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (!IsTargetAsset(asset))
        {
            return null;
        }

        var model = asset.Modele ?? string.Empty;
        var isDefective = asset.SousEtat == SousEtat.Defectueux;
        var isInRevalorisation = asset.SousEtat == SousEtat.Revalorisation;
        var isInRepair = IsRepairSubstate(asset.SousEtat);

        if (IsL13G1(model))
        {
            if (!isDefective && !isInRevalorisation)
            {
                return null;
            }

            if (!isDefective && isInRevalorisation)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = "L13 G1 8 Go en Revalorisation sans être défectueux : requalifier en Disponible Re-Use.",
                    EstBloquant = true
                };
            }

            if (isDefective && !isInRevalorisation)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = "L13 G1 8 Go défectueux : doit aller en Revalorisation avec commentaire de panne.",
                    EstBloquant = true
                };
            }

            return null;
        }

        var isL13 = model.Contains("L13", StringComparison.OrdinalIgnoreCase);
        var isL14 = model.Contains("L14", StringComparison.OrdinalIgnoreCase);

        if (!isL13 && !isL14)
        {
            return null;
        }

        if (!isDefective && !isInRevalorisation && !isInRepair)
        {
            return null;
        }

        if (!asset.DateRenouvellement.HasValue)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = $"{BuildModelLabel(model)} 8 Go : date de renouvellement manquante, impossible de trancher entre Revalorisation et Réparation.",
                EstBloquant = false
            };
        }

        var renewalYear = asset.DateRenouvellement.Value.Year;
        var modelLabel = BuildModelLabel(model);

        if (renewalYear <= 2027)
        {
            if (!isDefective && isInRevalorisation)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = $"{modelLabel} 8 Go en Revalorisation sans être défectueux : requalifier en Disponible Re-Use.",
                    EstBloquant = true
                };
            }

            if (isDefective && !isInRevalorisation)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = $"{modelLabel} 8 Go défectueux avec renouvellement {renewalYear} : doit aller en Revalorisation avec commentaire de panne.",
                    EstBloquant = true
                };
            }

            return null;
        }

        if (renewalYear >= 2028)
        {
            if (isInRevalorisation)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = $"{modelLabel} 8 Go défectueux avec renouvellement {renewalYear} : doit aller en Réparation, pas en Revalorisation.",
                    EstBloquant = true
                };
            }

            if (isDefective && !isInRepair)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = $"{modelLabel} 8 Go défectueux avec renouvellement {renewalYear} : doit être classé en Réparation.",
                    EstBloquant = true
                };
            }
        }

        return null;
    }

    private static bool IsTargetAsset(HardwareAsset asset)
    {
        var isComputer = asset.Categorie == CategorieEquipement.Ordinateur;
        var isLenovo = asset.Fabricant?.Contains("lenovo", StringComparison.OrdinalIgnoreCase) == true;
        var is8Go = asset.RamGo == 8;
        var model = asset.Modele ?? string.Empty;
        var isL13OrL14 = model.Contains("L13", StringComparison.OrdinalIgnoreCase)
                      || model.Contains("L14", StringComparison.OrdinalIgnoreCase);

        return isComputer && isLenovo && is8Go && isL13OrL14;
    }

    private static bool IsL13G1(string model)
    {
        if (!model.Contains("L13", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return L13G1Markers.Any(marker => model.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRepairSubstate(SousEtat sousEtat)
    {
        return sousEtat.ToString().Contains("Repar", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildModelLabel(string model)
    {
        if (IsL13G1(model))
        {
            return "L13 G1";
        }

        if (model.Contains("L13", StringComparison.OrdinalIgnoreCase))
        {
            return "L13 G2";
        }

        if (model.Contains("L14", StringComparison.OrdinalIgnoreCase))
        {
            return "L14";
        }

        return "Lenovo";
    }
}