using FluentAssertions;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.Tests.UseCases;

public sealed class FilterAssetsUseCaseTests
{
    [Fact]
    public void Execute_FiltersBySearchText_OnAssetTagModeleOrCommentaire()
    {
        var useCase = new FilterAssetsUseCase();
        var assets = CreateAssets();
        var criteria = new FilterCriteria(SearchText: "L14");

        var result = useCase.Execute(assets, criteria);

        result.Should().HaveCount(1);
        result[0].AssetTag.Should().Be("A-001");
    }

    [Fact]
    public void Execute_FiltersByNiveauMin_ExcludesLowerSeverities()
    {
        var useCase = new FilterAssetsUseCase();
        var assets = CreateAssets();
        var criteria = new FilterCriteria(NiveauMin: NiveauAnomalie.Avertissement);

        var result = useCase.Execute(assets, criteria);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Evaluation != null &&
                                          (a.Evaluation.Niveau == NiveauAnomalie.Avertissement ||
                                           a.Evaluation.Niveau == NiveauAnomalie.Erreur ||
                                           a.Evaluation.Niveau == NiveauAnomalie.Bloquant));
    }

    private static IReadOnlyList<HardwareAsset> CreateAssets()
    {
        return new List<HardwareAsset>
        {
            new()
            {
                AssetTag = "A-001",
                Categorie = CategorieEquipement.Ordinateur,
                Fabricant = "Lenovo",
                Modele = "ThinkPad L14",
                Commentaire = "OK",
                Evaluation = new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Info,
                    RegleDeclenchee = "RuleA",
                    Message = "Info",
                    EstBloquant = false
                }
            },
            new()
            {
                AssetTag = "A-002",
                Categorie = CategorieEquipement.Ordinateur,
                Fabricant = "Dell",
                Modele = "Latitude",
                Commentaire = "Panne",
                Evaluation = new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Avertissement,
                    RegleDeclenchee = "RuleB",
                    Message = "Warn",
                    EstBloquant = false
                }
            },
            new()
            {
                AssetTag = "A-003",
                Categorie = CategorieEquipement.Peripherique,
                Fabricant = "HP",
                Modele = "Dock",
                Commentaire = "HS",
                Evaluation = new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = "RuleC",
                    Message = "Error",
                    EstBloquant = true
                }
            },
            new()
            {
                AssetTag = "A-004",
                Categorie = CategorieEquipement.Autre,
                Fabricant = "Acer",
                Modele = "X",
                Commentaire = "N/A",
                Evaluation = null
            }
        };
    }
}
