using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Exécute un ensemble ordonné de règles métier sur un équipement.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyList<IRule> _rules;

    public RuleEngine(IEnumerable<IRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    public EvaluationResult? EvaluateAll(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(asset);

            if (result is null)
            {
                continue;
            }

            if (rule.IsOverride && !result.EstBloquant)
            {
                return null;
            }

            return result;
        }

        return null;
    }
}