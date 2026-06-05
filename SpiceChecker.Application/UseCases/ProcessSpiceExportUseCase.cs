using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Rules;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Orchestre l'import d'un export SPICE puis l'évaluation des équipements par les règles métier.
/// </summary>
public sealed class ProcessSpiceExportUseCase : IProcessSpiceExportUseCase
{
    private readonly IHardwareImportService _hardwareImportService;
    private readonly IReadOnlyList<IRule> _rules;

    /// <summary>
    /// Initialise une nouvelle instance du cas d'usage.
    /// </summary>
    /// <param name="hardwareImportService">Service d'import Excel.</param>
    /// <param name="rules">Règles métier à appliquer dans l'ordre.</param>
    public ProcessSpiceExportUseCase(IHardwareImportService hardwareImportService, IEnumerable<IRule> rules)
    {
        _hardwareImportService = hardwareImportService ?? throw new ArgumentNullException(nameof(hardwareImportService));
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    /// <summary>
    /// Exécute l'import puis l'évaluation de chaque équipement.
    /// </summary>
    /// <param name="excelStream">Flux Excel source.</param>
    /// <param name="progress">Canal de progression textuelle facultatif.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La liste des équipements avec évaluation renseignée.</returns>
    public async Task<IReadOnlyList<HardwareAsset>> ExecuteAsync(
        Stream excelStream,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var importedAssets = await _hardwareImportService.ImportAsync(excelStream, progress, cancellationToken);
        var ruleEngine = new RuleEngine(_rules);

        var total = importedAssets.Count;
        var evaluatedAssets = new List<HardwareAsset>(total);

        for (var i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var asset = importedAssets[i];
            var evaluation = ruleEngine.EvaluateAll(asset);
            var evaluated = asset with { Evaluation = evaluation };

            evaluatedAssets.Add(evaluated);
            progress?.Report($"Évaluation : {i + 1}/{total}");
        }

        return evaluatedAssets.AsReadOnly();
    }
}
