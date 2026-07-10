using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Validation;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Vérifie la cohérence minimale d'un équipement en Revalorisation.
/// </summary>
public sealed class RevalorisationRule : IRule
{
    private readonly IDefectCommentValidator _defectCommentValidator;

    public RevalorisationRule(IDefectCommentValidator defectCommentValidator)
    {
        _defectCommentValidator = defectCommentValidator ?? throw new ArgumentNullException(nameof(defectCommentValidator));
    }

    public string Name => "RevalorisationRule";

    public bool IsOverride => false;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.Categorie == CategorieEquipement.EquipementReseau)
        {
            return null;
        }

        if (asset.SousEtat != SousEtat.Revalorisation)
        {
            return null;
        }

        var validation = _defectCommentValidator.Validate(asset.Commentaire, asset.SousEtat);
        if (!validation.IsValid)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = Name,
                Message = validation.ErrorMessage,
                EstBloquant = true
            };
        }

        return null;
    }
}