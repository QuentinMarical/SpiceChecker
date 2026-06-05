using SpiceChecker.Application.Services;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Implémentation WinForms de la sélection de fichier.
/// </summary>
public sealed class WinFormsFilePickerService : IFilePickerService
{
    /// <inheritdoc />
    public Task<Stream?> PickFileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var dialog = new OpenFileDialog
        {
            Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Tous les fichiers (*.*)|*.*",
            Title = "Sélectionner un export SPICE",
            CheckFileExists = true,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(dialog.FileName);
        return Task.FromResult<Stream?>(stream);
    }
}
