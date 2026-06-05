using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.UseCases;

/// <summary>
/// Critères de filtrage des équipements évalués.
/// </summary>
public sealed record FilterCriteria(
    string? SearchText = null,
    CategorieEquipement? Categorie = null,
    NiveauAnomalie? NiveauMin = null,
    string? Fabricant = null);
