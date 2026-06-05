using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Rules;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Réévalue un équipement avec les règles métier configurées.
/// </summary>
public sealed class ReevaluateAssetUseCase : IReevaluateAssetUseCase
{
    private readonly RuleEngine _ruleEngine;

    public ReevaluateAssetUseCase(IEnumerable<IRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _ruleEngine = new RuleEngine(rules);
    }

    /// <summary>
    /// Retourne une nouvelle instance d'équipement avec sa nouvelle évaluation.
    /// </summary>
    /// <param name="asset">Équipement à réévaluer.</param>
    /// <returns>Équipement réévalué.</returns>
    public HardwareAsset Execute(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var evaluation = _ruleEngine.EvaluateAll(asset);
        return asset with { Evaluation = evaluation };
    }
}
