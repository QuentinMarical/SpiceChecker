using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Entities;

/// <summary>
/// Représente le résultat d'évaluation d'une règle métier sur un équipement.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>
    /// Niveau de sévérité de l'anomalie détectée.
    /// </summary>
    public NiveauAnomalie Niveau { get; init; }

    /// <summary>
    /// Nom unique de la règle ayant produit ce résultat.
    /// </summary>
    public string RegleDeclenchee { get; init; } = string.Empty;

    /// <summary>
    /// Message fonctionnel destiné à l'utilisateur ou à l'audit.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Indique si le résultat bloque la suite du traitement.
    /// </summary>
    public bool EstBloquant { get; init; }
}
