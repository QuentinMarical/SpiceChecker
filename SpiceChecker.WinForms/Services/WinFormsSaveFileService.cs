using SpiceChecker.Application.Services;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Implémentation WinForms du service de sélection du chemin de sauvegarde.
/// </summary>
public sealed class WinFormsSaveFileService : ISaveFileService
{
    public Task<string?> GetSaveFilePathAsync(string defaultFileName, string filter)
    {
        using var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = filter,
            OverwritePrompt = true,
            RestoreDirectory = true
        };

        return Task.FromResult(dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null);
    }
}
