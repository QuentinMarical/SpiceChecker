using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Entities;

/// <summary>
/// Représente un équipement matériel du stock, tel qu'importé et évalué par le domaine.
/// </summary>
public sealed record HardwareAsset
{
    /// <summary>
    /// Identifiant d'inventaire de l'équipement.
    /// </summary>
    public string AssetTag { get; init; } = string.Empty;

    /// <summary>
    /// Catégorie métier de l'équipement.
    /// </summary>
    public CategorieEquipement Categorie { get; init; } = CategorieEquipement.Autre;

    /// <summary>
    /// Fabricant de l'équipement.
    /// </summary>
    public string Fabricant { get; init; } = string.Empty;

    /// <summary>
    /// Modèle de l'équipement.
    /// </summary>
    public string Modele { get; init; } = string.Empty;

    /// <summary>
    /// Quantité de RAM en Go, si connue.
    /// </summary>
    public int? RamGo { get; init; }

    /// <summary>
    /// Date d'acquisition de l'équipement, si disponible.
    /// </summary>
    public DateTime? DateAcquisition { get; init; }

    /// <summary>
    /// Date de renouvellement de l'équipement, si disponible.
    /// </summary>
    public DateTime? DateRenouvellement { get; init; }

    /// <summary>
    /// Date de dernière modification du sous-état, si disponible.
    /// </summary>
    public DateTime? DateDerniereModifSousEtat { get; init; }

    /// <summary>
    /// Sous-état métier courant de l'équipement.
    /// </summary>
    public SousEtat SousEtat { get; init; } = SousEtat.Autre;

    /// <summary>
    /// Commentaire libre associé à l'équipement.
    /// 
    /// </summary>
    public string Commentaire { get; init; } = string.Empty;

    /// <summary>
    /// Résultat d'évaluation courant de l'équipement.
    /// Null par défaut tant qu'aucune règle n'a été exécutée.
    /// </summary>
    public EvaluationResult? Evaluation { get; init; } = null;
}
