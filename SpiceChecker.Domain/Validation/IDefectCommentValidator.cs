using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Validation;

/// <summary>
/// Définit le contrat de validation d'un commentaire de panne.
/// </summary>
public interface IDefectCommentValidator
{
    /// <summary>
    /// Valide un commentaire selon le sous-état métier de l'équipement.
    /// </summary>
    /// <param name="commentaire">Commentaire à valider.</param>
    /// <param name="sousEtat">Sous-état de l'équipement.</param>
    /// <returns>Le résultat de validation.</returns>
    ValidationResult Validate(string commentaire, SousEtat sousEtat);
}
