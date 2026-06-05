using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Contrat de filtrage des équipements évalués.
/// </summary>
public interface IFilterAssetsUseCase
{
    /// <summary>
    /// Filtre les équipements selon les critères fournis.
    /// </summary>
    IReadOnlyList<HardwareAsset> Execute(IReadOnlyList<HardwareAsset> assets, FilterCriteria criteria);
}
