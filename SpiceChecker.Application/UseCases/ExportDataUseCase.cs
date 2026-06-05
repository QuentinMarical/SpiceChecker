using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Orchestre l'export des données filtrées vers un fichier CSV ou Excel.
/// </summary>
public sealed class ExportDataUseCase : IExportDataUseCase
{
    private readonly IExportService _exportService;
    private readonly ISaveFileService _saveFileService;

    public ExportDataUseCase(IExportService exportService, ISaveFileService saveFileService)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _saveFileService = saveFileService ?? throw new ArgumentNullException(nameof(saveFileService));
    }

    public async Task ExecuteCsvExportAsync(IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assets);

        var path = await _saveFileService.GetSaveFilePathAsync("SpiceChecker-Export.csv", "CSV (*.csv)|*.csv");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await _exportService.ExportCsvAsync(stream, assets, ct);
    }

    public async Task ExecuteXlsxExportAsync(IReadOnlyList<HardwareAsset> assets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assets);

        var path = await _saveFileService.GetSaveFilePathAsync("SpiceChecker-Export.xlsx", "Excel (*.xlsx)|*.xlsx");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await _exportService.ExportXlsxAsync(stream, assets, ct);
    }
}
