using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Contrat d'orchestration de l'export des données filtrées.
/// </summary>
public interface IExportDataUseCase
{
    Task ExecuteCsvExportAsync(IReadOnlyList<HardwareAsset> assets, CancellationToken ct);

    Task ExecuteXlsxExportAsync(IReadOnlyList<HardwareAsset> assets, CancellationToken ct);
}
