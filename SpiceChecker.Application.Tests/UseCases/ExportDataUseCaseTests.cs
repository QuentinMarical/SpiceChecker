using FluentAssertions;
using Moq;
using SpiceChecker.Application.Services;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Application.Tests.UseCases;

public sealed class ExportDataUseCaseTests
{
    private static HardwareAsset BuildAsset() => new()
    {
        AssetTag = "TAG001",
        Categorie = CategorieEquipement.Ordinateur,
        Fabricant = "Dell",
        Modele = "Latitude 5420",
        RamGo = 16,
        SousEtat = SousEtat.Disponible,
        Commentaire = "Test"
    };

    [Fact]
    public async Task ExecuteXlsxExportAsync_CallsExportService_WhenPathIsSelected()
    {
        // Arrange
        var assets = new[] { BuildAsset() };
        var exportServiceMock = new Mock<IExportService>();
        var saveFileServiceMock = new Mock<ISaveFileService>();
        saveFileServiceMock
            .Setup(s => s.GetSaveFilePathAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Path.GetTempFileName());

        exportServiceMock
            .Setup(e => e.ExportXlsxAsync(It.IsAny<Stream>(), It.IsAny<IReadOnlyList<HardwareAsset>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var useCase = new ExportDataUseCase(exportServiceMock.Object, saveFileServiceMock.Object);

        // Act
        await useCase.ExecuteXlsxExportAsync(assets, CancellationToken.None);

        // Assert
        exportServiceMock.Verify(e => e.ExportXlsxAsync(It.IsAny<Stream>(), assets, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCsvExportAsync_DoesNotCallExportService_WhenUserCancels()
    {
        // Arrange
        var assets = new[] { BuildAsset() };
        var exportServiceMock = new Mock<IExportService>(MockBehavior.Strict);
        var saveFileServiceMock = new Mock<ISaveFileService>();
        saveFileServiceMock
            .Setup(s => s.GetSaveFilePathAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var useCase = new ExportDataUseCase(exportServiceMock.Object, saveFileServiceMock.Object);

        // Act
        await useCase.ExecuteCsvExportAsync(assets, CancellationToken.None);

        // Assert
        exportServiceMock.VerifyNoOtherCalls();
    }
}
