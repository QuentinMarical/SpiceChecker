namespace SpiceChecker.Domain.Enums;

/// <summary>
/// Libellés d'affichage français des énumérations métier.
/// </summary>
public static class EnumLibelles
{
    /// <summary>
    /// Retourne le libellé SPICE du sous-état.
    /// </summary>
    public static string Libelle(this SousEtat sousEtat) => sousEtat switch
    {
        SousEtat.DisponibleNeuf => "Disponible neuf",
        SousEtat.Disponible => "Disponible Re-Use",
        SousEtat.Revalorisation => "Revalorisation (Déclassé, Retour loueur)",
        SousEtat.ReserveMasterise => "Réservé/Masterisé",
        SousEtat.ABlanchir => "A blanchir",
        SousEtat.EnAttenteDeDon => "En attente de don",
        SousEtat.RepriseEnAttente => "Reprise en attente",
        SousEtat.Defectueux => "Défectueux",
        SousEtat.EnReparation => "En réparation",
        _ => "Autre"
    };

    /// <summary>
    /// Retourne le libellé français de la catégorie d'équipement.
    /// </summary>
    public static string Libelle(this CategorieEquipement categorie) => categorie switch
    {
        CategorieEquipement.Ordinateur => "Ordinateur",
        CategorieEquipement.EquipementReseau => "Équipement réseau",
        CategorieEquipement.Serveur => "Serveur",
        _ => categorie.ToString()
    };
}
