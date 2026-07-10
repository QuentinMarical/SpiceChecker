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
    /// Indique qu'une règle peut court-circuiter les suivantes lorsqu'elle s'applique sans anomalie.
    /// </summary>
    bool IsOverride => false;

    /// <summary>
    /// Évalue l'équipement et retourne un résultat d'anomalie si la règle est déclenchée.
    /// Retourne null si aucune anomalie n'est détectée.
    /// </summary>
    EvaluationResult? Evaluate(HardwareAsset asset);
}