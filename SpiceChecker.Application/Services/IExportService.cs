using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.Services;

/// <summary>
/// Exporte une liste d'équipements dans différents formats.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exporte les données au format CSV.
    /// </summary>
    Task ExportCsvAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct);

    /// <summary>
    /// Exporte les données au format Excel.
    /// </summary>
    Task ExportXlsxAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct);
}
