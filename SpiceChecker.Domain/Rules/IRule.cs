using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Définit un contrat de règle métier appliquée à un équipement matériel.
/// </summary>
public interface IRule
{
    /// <summary>
    /// Nom de la règle, utilisé pour l'identification et le diagnostic.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Évalue l'équipement et retourne un résultat d'anomalie si la règle est déclenchée.
    /// Retourne <see langword="null"/> si aucune anomalie n'est détectée.
    /// </summary>
    /// <param name="asset">Équipement à évaluer.</param>
    /// <returns>Le résultat d'évaluation, ou <see langword="null"/> si la règle ne déclenche pas.</returns>
    EvaluationResult? Evaluate(HardwareAsset asset);
}
