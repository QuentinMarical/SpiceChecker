using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Infrastructure.Export;

/// <summary>
/// Service d'export unifié utilisé par le cas d'usage d'orchestration.
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly CsvExportService _csvExportService;
    private readonly XlsxExportService _xlsxExportService;

    public ExportService(CsvExportService csvExportService, XlsxExportService xlsxExportService)
    {
        _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
        _xlsxExportService = xlsxExportService ?? throw new ArgumentNullException(nameof(xlsxExportService));
    }

    public Task ExportCsvAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
        => _csvExportService.ExportCsvAsync(stream, assets, ct);

    public Task ExportXlsxAsync(Stream stream, IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
        => _xlsxExportService.ExportXlsxAsync(stream, assets, ct);
}
