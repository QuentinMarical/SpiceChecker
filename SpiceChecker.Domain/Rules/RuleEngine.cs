using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Exécute un ensemble ordonné de règles métier sur un équipement.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyList<IRule> _rules;

    /// <summary>
    /// Initialise une nouvelle instance du moteur de règles.
    /// </summary>
    /// <param name="rules">Règles à exécuter, dans l'ordre d'évaluation.</param>
    /// <exception cref="ArgumentNullException">Levée si <paramref name="rules"/> est null.</exception>
    public RuleEngine(IEnumerable<IRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    /// <summary>
    /// Évalue toutes les règles dans l'ordre et s'arrête à la première anomalie détectée.
    /// </summary>
    /// <param name="asset">Équipement à évaluer.</param>
    /// <returns>
    /// Le premier <see cref="EvaluationResult"/> non null retourné par une règle,
    /// ou <see langword="null"/> si aucune anomalie n'est détectée.
    /// </returns>
    /// <exception cref="ArgumentNullException">Levée si <paramref name="asset"/> est null.</exception>
    public EvaluationResult? EvaluateAll(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(asset);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
