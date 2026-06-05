using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Détecte les équipements en revalorisation, avec exclusion explicite de la catégorie réseau.
/// </summary>
public sealed class RevalorisationRule : IRule
{
    /// <inheritdoc />
    public string Name => "RevalorisationRule";

    /// <inheritdoc />
    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.Categorie == CategorieEquipement.EquipementReseau)
        {
            return null;
        }

        if (asset.SousEtat == SousEtat.Revalorisation)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Info,
                RegleDeclenchee = Name,
                Message = "Revalorisation à envisager pour cet équipement.",
                EstBloquant = false
            };
        }

        return null;
    }
}
