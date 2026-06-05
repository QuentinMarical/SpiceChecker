using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.Services;

/// <summary>
/// Résultat de l'édition d'un équipement via une boîte de dialogue.
/// </summary>
/// <param name="SousEtat">Nouveau sous-état.</param>
/// <param name="Commentaire">Nouveau commentaire.</param>
public sealed record EditAssetDialogResult(SousEtat SousEtat, string Commentaire);
