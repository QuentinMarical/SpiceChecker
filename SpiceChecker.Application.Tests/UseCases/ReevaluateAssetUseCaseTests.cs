using FluentAssertions;
using Moq;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;

namespace SpiceChecker.Application.Tests.UseCases;

public sealed class ReevaluateAssetUseCaseTests
{
    private static readonly EvaluationResult FakeEvaluation = new()
    {
        Niveau = NiveauAnomalie.Avertissement,
        RegleDeclenchee = "FakeRule",
        Message = "Anomalie fictive",
        EstBloquant = false
    };

    private static HardwareAsset BuildAsset(string assetTag = "TAG001") => new()
    {
        AssetTag = assetTag,
        Categorie = CategorieEquipement.Ordinateur,
        Fabricant = "Dell",
        Modele = "Latitude 5420",
        RamGo = 16,
        SousEtat = SousEtat.Disponible,
        Commentaire = "RAS",
        DateAcquisition = new DateTime(2022, 1, 10),
        DateRenouvellement = new DateTime(2025, 1, 10),
        Evaluation = null
    };

    [Fact]
    public void Execute_ReturnsNewInstance_WhenAssetIsEvaluated()
    {
        // Arrange
        var ruleMock = new Mock<IRule>();
        ruleMock.Setup(r => r.Evaluate(It.IsAny<HardwareAsset>())).Returns(FakeEvaluation);

        var useCase = new ReevaluateAssetUseCase(new[] { ruleMock.Object });
        var original = BuildAsset();

        // Act
        var result = useCase.Execute(original);

        // Assert — immuabilité : nouvel objet
        result.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Execute_SetsEvaluation_WhenRuleMatches()
    {
        // Arrange
        var ruleMock = new Mock<IRule>();
        ruleMock.Setup(r => r.Evaluate(It.IsAny<HardwareAsset>())).Returns(FakeEvaluation);

        var useCase = new ReevaluateAssetUseCase(new[] { ruleMock.Object });
        var original = BuildAsset();

        // Act
        var result = useCase.Execute(original);

        // Assert — évaluation renseignée
        result.Evaluation.Should().NotBeNull();
        result.Evaluation.Should().Be(FakeEvaluation);
    }

    [Fact]
    public void Execute_PreservesAllOtherProperties_WhenAssetIsEvaluated()
    {
        // Arrange
        var ruleMock = new Mock<IRule>();
        ruleMock.Setup(r => r.Evaluate(It.IsAny<HardwareAsset>())).Returns(FakeEvaluation);

        var useCase = new ReevaluateAssetUseCase(new[] { ruleMock.Object });
        var original = BuildAsset("TAG999");

        // Act
        var result = useCase.Execute(original);

        // Assert — toutes les autres propriétés sont strictement identiques
        result.AssetTag.Should().Be(original.AssetTag);
        result.Categorie.Should().Be(original.Categorie);
        result.Fabricant.Should().Be(original.Fabricant);
        result.Modele.Should().Be(original.Modele);
        result.RamGo.Should().Be(original.RamGo);
        result.SousEtat.Should().Be(original.SousEtat);
        result.Commentaire.Should().Be(original.Commentaire);
        result.DateAcquisition.Should().Be(original.DateAcquisition);
        result.DateRenouvellement.Should().Be(original.DateRenouvellement);
        result.DateDerniereModifSousEtat.Should().Be(original.DateDerniereModifSousEtat);
    }
}
