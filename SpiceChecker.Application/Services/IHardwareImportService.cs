using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.Services;

/// <summary>
/// Définit le contrat d'import des équipements depuis un flux Excel.
/// </summary>
public interface IHardwareImportService
{
    /// <summary>
    /// Importe des équipements depuis un flux Excel.
    /// </summary>
    /// <param name="excelStream">Flux du fichier Excel source.</param>
    /// <param name="progress">Canal facultatif de progression textuelle.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La liste des équipements importés.</returns>
    Task<IReadOnlyList<HardwareAsset>> ImportAsync(
        Stream excelStream,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
