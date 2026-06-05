namespace SpiceChecker.Domain.Enums;

/// <summary>
/// Représente le niveau de sévérité d'une anomalie détectée.
/// </summary>
public enum NiveauAnomalie
{
    /// <summary>
    /// Information non bloquante.
    /// </summary>
    Info,

    /// <summary>
    /// Avertissement nécessitant une vérification.
    /// </summary>
    Avertissement,

    /// <summary>
    /// Erreur métier nécessitant une correction.
    /// </summary>
    Erreur,

    /// <summary>
    /// Anomalie critique empêchant la poursuite du processus.
    /// </summary>
    Bloquant
}
