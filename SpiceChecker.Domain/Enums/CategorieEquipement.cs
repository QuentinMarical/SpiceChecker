namespace SpiceChecker.Domain.Enums;

/// <summary>
/// Représente les catégories métier d'équipements traitées par SpiceChecker.
/// </summary>
public enum CategorieEquipement
{
    /// <summary>
    /// Matériel informatique de type ordinateur.
    /// </summary>
    Ordinateur,

    /// <summary>
    /// Équipement réseau (switch, routeur, etc.).
    /// </summary>
    EquipementReseau,

    /// <summary>
    /// Périphérique informatique (écran, clavier, etc.).
    /// </summary>
    Peripherique,

    /// <summary>
    /// Catégorie non classifiée explicitement.
    /// </summary>
    Autre
}
