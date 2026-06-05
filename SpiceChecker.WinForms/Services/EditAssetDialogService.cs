using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Enums;
using SpiceChecker.WinForms.Views;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Service WinForms d'édition du sous-état et du commentaire.
/// </summary>
public sealed class EditAssetDialogService : IEditAssetDialogService
{
    public Task<EditAssetDialogResult?> EditAsync(
        SousEtat currentSousEtat,
        string currentCommentaire,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var dialog = new EditSubStateForm(currentSousEtat, currentCommentaire);
        var result = dialog.ShowDialog();

        if (result != DialogResult.OK)
        {
            return Task.FromResult<EditAssetDialogResult?>(null);
        }

        var payload = new EditAssetDialogResult(dialog.ResultatSousEtat, dialog.ResultatCommentaire);
        return Task.FromResult<EditAssetDialogResult?>(payload);
    }
}
