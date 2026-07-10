namespace SpiceChecker.Domain.Enums;

/// <summary>
/// Représente le sous-état métier d'un équipement (aligné sur les sous-états SPICE / ServiceNow).
/// </summary>
public enum SousEtat
{
    /// <summary>
    /// Équipement disponible neuf.
    /// </summary>
    DisponibleNeuf,

    /// <summary>
    /// Équipement disponible Re-Use (réutilisable immédiatement).
    /// </summary>
    Disponible,

    /// <summary>
    /// Équipement orienté revalorisation (Déclassé, Retour loueur).
    /// </summary>
    Revalorisation,

    /// <summary>
    /// Équipement réservé / masterisé (stock bloqué pour un usage spécifique).
    /// </summary>
    ReserveMasterise,

    /// <summary>
    /// Équipement à blanchir avant remise en Disponible Re-Use.
    /// </summary>
    ABlanchir,

    /// <summary>
    /// Équipement en attente de don (sortie de parc).
    /// </summary>
    EnAttenteDeDon,

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
