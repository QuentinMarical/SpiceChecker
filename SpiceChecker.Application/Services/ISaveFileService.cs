namespace SpiceChecker.Application.Services;

/// <summary>
/// Fournit un chemin de sauvegarde choisi par l'utilisateur.
/// </summary>
public interface ISaveFileService
{
    /// <summary>
    /// Ouvre un sélecteur d'emplacement de fichier.
    /// </summary>
    /// <param name="defaultFileName">Nom de fichier par défaut.</param>
    /// <param name="filter">Filtre de type de fichier.</param>
    /// <returns>Le chemin sélectionné, ou <see langword="null"/> si l'utilisateur annule.</returns>
    Task<string?> GetSaveFilePathAsync(string defaultFileName, string filter);
}
