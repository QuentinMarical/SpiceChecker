using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Contrat d'orchestration de l'import et de l'évaluation d'un export SPICE.
/// </summary>
public interface IProcessSpiceExportUseCase
{
    /// <summary>
    /// Exécute l'import puis l'évaluation de chaque équipement.
    /// </summary>
    Task<IReadOnlyList<HardwareAsset>> ExecuteAsync(
        Stream excelStream,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
