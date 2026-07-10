using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Règles Lenovo L13/L14 8 Go (consignes Enedis depuis le 1er avril 2026) :
/// - Fonctionnels => Disponible Re-Use, peu importe la date de renouvellement (plus de remise en stock classique).
/// - L13 G1 défectueux => Revalorisation obligatoire (avec commentaire de panne).
/// - L13 G2 / L14 défectueux => renouvellement 2027 ou avant => Revalorisation, 2028 ou après => Réparation.
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
        var modelLabel = BuildModelLabel(model);

        switch (asset.SousEtat)
        {
            case SousEtat.DisponibleNeuf:
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Avertissement,
                    RegleDeclenchee = Name,
                    Message = $"{modelLabel} 8 Go en Disponible neuf : plus de remise en stock classique, un {modelLabel} fonctionnel doit être en Disponible Re-Use (incident modèle équivalent/inférieur ou contrat court).",
                    EstBloquant = false
                };

            case SousEtat.Defectueux:
                return EvaluateDefectueux(asset, modelLabel, model);

            case SousEtat.Revalorisation:
                return EvaluateRevalorisation(asset, modelLabel, model);

            default:
                return null;
        }
    }

    private EvaluationResult? EvaluateDefectueux(HardwareAsset asset, string modelLabel, string model)
    {
        if (IsL13G1(model))
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = "L13 G1 8 Go défectueux : à passer en Revalorisation, peu importe la date de renouvellement. Commentaire de panne obligatoire.",
                EstBloquant = false
            };
        }

        if (!asset.DateRenouvellement.HasValue)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = $"{modelLabel} 8 Go défectueux : date de renouvellement absente de l'export, vérifier dans ServiceNow (2027 ou avant → Revalorisation, 2028 ou après → Réparation). Commentaire de panne obligatoire.",
                EstBloquant = false
            };
        }

        var renewalYear = asset.DateRenouvellement.Value.Year;
        if (renewalYear <= 2027)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = $"{modelLabel} 8 Go défectueux, renouvellement {renewalYear} : à passer en Revalorisation avec commentaire de panne.",
                EstBloquant = false
            };
        }

        return new EvaluationResult
        {
            Niveau = NiveauAnomalie.Avertissement,
            RegleDeclenchee = Name,
            Message = $"{modelLabel} 8 Go défectueux, renouvellement {renewalYear} : à renvoyer en Réparation (pas de Revalorisation). Commentaire de panne obligatoire.",
            EstBloquant = false
        };
    }

    private EvaluationResult? EvaluateRevalorisation(HardwareAsset asset, string modelLabel, string model)
    {
        // L13 G1 défectueux en Revalorisation : conforme, la justification de panne
        // est contrôlée par les règles Revalorisation.
        if (IsL13G1(model))
        {
            return null;
        }

        if (asset.DateRenouvellement.HasValue && asset.DateRenouvellement.Value.Year >= 2028)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = Name,
                Message = $"{modelLabel} 8 Go en Revalorisation avec renouvellement {asset.DateRenouvellement.Value.Year} : doit partir en Réparation, pas en Revalorisation.",
                EstBloquant = true
            };
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
