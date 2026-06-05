namespace SpiceChecker.Domain.Enums;

/// <summary>
/// Représente le sous-état métier d'un équipement.
/// </summary>
public enum SousEtat
{
    /// <summary>
    /// Équipement disponible neuf.
    /// </summary>
    DisponibleNeuf,

    /// <summary>
    /// Équipement disponible (re-use ou disponibilité standard).
    /// </summary>
    Disponible,

    /// <summary>
    /// Équipement orienté revalorisation.
    /// </summary>
    Revalorisation,

    /// <summary>
    /// Équipement en attente de reprise.
    /// </summary>
    RepriseEnAttente,

    /// <summary>
    /// Équipement défectueux.
    /// </summary>
    Defectueux,

    /// <summary>
    /// Équipement en réparation.
    /// </summary>
    EnReparation,

    /// <summary>
    /// Sous-état non classifié explicitement.
    /// </summary>
    Autre
}
