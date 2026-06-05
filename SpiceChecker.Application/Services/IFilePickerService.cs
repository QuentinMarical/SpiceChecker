namespace SpiceChecker.Application.Services;

/// <summary>
/// Fournit un flux de fichier sélectionné par l'utilisateur.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Ouvre un sélecteur de fichier et retourne le flux du fichier choisi.
    /// </summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Un flux en lecture seule, ou <see langword="null"/> si l'utilisateur annule.</returns>
    Task<Stream?> PickFileAsync(CancellationToken cancellationToken = default);
}
