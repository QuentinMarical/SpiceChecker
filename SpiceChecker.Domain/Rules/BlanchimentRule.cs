using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// « A blanchir » est une étape intermédiaire, pas une destination finale :
/// après blanchiment, l'équipement doit repasser en Disponible Re-Use.
/// </summary>
public sealed class BlanchimentRule : IRule
{
    public string Name => "BlanchimentRule";

    public bool IsOverride => false;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.SousEtat != SousEtat.ABlanchir)
        {
            return null;
        }

        return new EvaluationResult
        {
            Niveau = NiveauAnomalie.Info,
            RegleDeclenchee = Name,
            Message = "À blanchir : après blanchiment, repasser l'équipement en Disponible Re-Use.",
            EstBloquant = false
        };
    }
}
