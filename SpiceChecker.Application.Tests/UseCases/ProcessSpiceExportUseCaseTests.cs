using FluentAssertions;
using Moq;
using SpiceChecker.Application.Services;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Rules;

namespace SpiceChecker.Application.Tests.UseCases;

public sealed class ProcessSpiceExportUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_EvaluatesImportedAssets_AndReportsProgress()
    {
        var importedAssets = new List<HardwareAsset>
        {
            new()
            {
                AssetTag = "A-001",
                Categorie = CategorieEquipement.Ordinateur,
                Fabricant = "Lenovo",
                Modele = "L14",
                RamGo = 16,
                SousEtat = SousEtat.Disponible
            },
            new()
            {
                AssetTag = "A-002",
                Categorie = CategorieEquipement.Serveur,
                Fabricant = "HP",
                Modele = "Dock",
                RamGo = null,
                SousEtat = SousEtat.Autre
            }
        };

        var importServiceMock = new Mock<IHardwareImportService>();
        importServiceMock
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedAssets);

        var progressMessages = new List<string>();
        var progress = new Progress<string>(m => progressMessages.Add(m));

        var useCase = new ProcessSpiceExportUseCase(importServiceMock.Object, new IRule[] { new HighRamLenovoRule() });

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await useCase.ExecuteAsync(stream, progress, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Evaluation.Should().NotBeNull();
        result[0].Evaluation!.RegleDeclenchee.Should().Be("HighRamLenovoRule");
        result[1].Evaluation.Should().BeNull();
        progressMessages.Should().Contain("Évaluation : 1/2");
        progressMessages.Should().Contain("Évaluation : 2/2");
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsOperationCanceledException_WhenCancellationRequested()
    {
        var importedAssets = new List<HardwareAsset>
        {
            new()
            {
                AssetTag = "A-001",
                Categorie = CategorieEquipement.Ordinateur,
                Fabricant = "Lenovo",
                Modele = "L14",
                RamGo = 16,
                SousEtat = SousEtat.Disponible
            }
        };

        var importServiceMock = new Mock<IHardwareImportService>();
        importServiceMock
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedAssets);

        var useCase = new ProcessSpiceExportUseCase(importServiceMock.Object, new IRule[] { new HighRamLenovoRule() });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var act = async () => await useCase.ExecuteAsync(stream, null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
