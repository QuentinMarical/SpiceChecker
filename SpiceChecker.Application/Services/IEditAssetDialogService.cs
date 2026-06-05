using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.Services;

/// <summary>
/// Service d'édition interactive des informations d'un équipement.
/// </summary>
public interface IEditAssetDialogService
{
    /// <summary>
    /// Ouvre une interface d'édition pour le sous-état et le commentaire.
    /// </summary>
    /// <param name="currentSousEtat">Sous-état actuel.</param>
    /// <param name="currentCommentaire">Commentaire actuel.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Les nouvelles valeurs si l'utilisateur valide, sinon <see langword="null"/>.</returns>
    Task<EditAssetDialogResult?> EditAsync(
        SousEtat currentSousEtat,
        string currentCommentaire,
        CancellationToken cancellationToken = default);
}
