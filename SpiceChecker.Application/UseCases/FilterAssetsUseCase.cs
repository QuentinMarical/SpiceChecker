using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Applique des filtres métier sur une liste d'équipements.
/// </summary>
public sealed class FilterAssetsUseCase : IFilterAssetsUseCase
{
    /// <summary>
    /// Filtre les équipements selon les critères fournis.
    /// </summary>
    /// <param name="assets">Liste source des équipements.</param>
    /// <param name="criteria">Critères de filtrage.</param>
    /// <returns>Liste filtrée.</returns>
    public IReadOnlyList<HardwareAsset> Execute(IReadOnlyList<HardwareAsset> assets, FilterCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(criteria);

        IEnumerable<HardwareAsset> query = assets;

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var search = criteria.SearchText.Trim();
            query = query.Where(a =>
                Contains(a.AssetTag, search)
                || Contains(a.Modele, search)
                || Contains(a.Fabricant, search)
                || Contains(a.Entrepot, search)
                || Contains(a.SousEtat.Libelle(), search)
                || Contains(a.Commentaire, search));
        }

        if (criteria.Categorie.HasValue)
        {
            query = query.Where(a => a.Categorie == criteria.Categorie.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Fabricant))
        {
            var fabricant = criteria.Fabricant.Trim();
            query = query.Where(a => Contains(a.Fabricant, fabricant));
        }

        if (criteria.AnomaliesOnly)
        {
            query = query.Where(a => a.Evaluation is not null);
        }

        if (criteria.NiveauMin.HasValue)
        {
            var threshold = GetSeverityRank(criteria.NiveauMin.Value);
            query = query.Where(a =>
                a.Evaluation is not null
                && GetSeverityRank(a.Evaluation.Niveau) >= threshold);
        }

        return query.ToList().AsReadOnly();
    }

    private static bool Contains(string? source, string value)
        => !string.IsNullOrWhiteSpace(source)
           && source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static int GetSeverityRank(NiveauAnomalie niveau) => niveau switch
    {
        NiveauAnomalie.Info => 0,
        NiveauAnomalie.Avertissement => 1,
        NiveauAnomalie.Erreur => 2,
        NiveauAnomalie.Bloquant => 3,
        _ => 0
    };
}
