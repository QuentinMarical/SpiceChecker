using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Critères de filtrage des équipements évalués.
/// </summary>
public sealed record FilterCriteria(
    string? SearchText = null,
    CategorieEquipement? Categorie = null,
    SousEtat? SousEtat = null,
    NiveauAnomalie? NiveauMin = null,
    string? Fabricant = null,
    bool AnomaliesOnly = false,
    bool ConformesOnly = false);
