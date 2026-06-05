using FluentAssertions;
using Moq;
using SpiceChecker.Application.Services;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.WinForms.ViewModels;

namespace SpiceChecker.WinForms.Tests.ViewModels;

public sealed class MainViewModelTests
{
    private readonly Mock<IExportDataUseCase> mockExportUseCase = new();
    private readonly Mock<IClipboardService> mockClipboardService = new();
    private readonly Mock<IClipboardExportFormatter> mockClipboardFormatter = new();
    private readonly Mock<IThemeService> mockThemeService = new();
    private readonly Mock<ISettingsService> mockSettingsService = new();

    private static readonly EvaluationResult FakeEvaluation = new()
    {
        Niveau = NiveauAnomalie.Avertissement,
        RegleDeclenchee = "FakeRule",
        Message = "Anomalie fictive",
        EstBloquant = false
    };

    private static HardwareAsset BuildAsset(string assetTag = "TEST001", SousEtat sousEtat = SousEtat.Disponible) => new()
    {
        AssetTag = assetTag,
        Categorie = CategorieEquipement.Ordinateur,
        Fabricant = "Dell",
        Modele = "Latitude 5420",
        SousEtat = sousEtat,
        Commentaire = string.Empty
    };

    private MainViewModel BuildViewModel(
        Mock<IProcessSpiceExportUseCase> processUseCaseMock,
        Mock<IFilterAssetsUseCase>? filterUseCaseMock = null,
        Mock<IReevaluateAssetUseCase>? reevaluateMock = null,
        Mock<IExportDataUseCase>? exportDataUseCaseMock = null,
        Mock<IFilePickerService>? filePickerMock = null,
        Mock<IEditAssetDialogService>? editDialogMock = null,
        Mock<IClipboardService>? clipboardMock = null,
        Mock<IClipboardExportFormatter>? clipboardFormatterMock = null,
        Mock<IThemeService>? themeServiceMock = null,
        Mock<ISettingsService>? settingsServiceMock = null)
    {
        filterUseCaseMock ??= BuildPassThroughFilterMock();
        reevaluateMock ??= new Mock<IReevaluateAssetUseCase>();
        exportDataUseCaseMock ??= new Mock<IExportDataUseCase>();
        filePickerMock ??= new Mock<IFilePickerService>();
        editDialogMock ??= new Mock<IEditAssetDialogService>();
        clipboardMock ??= mockClipboardService;
        clipboardFormatterMock ??= mockClipboardFormatter;
        themeServiceMock ??= mockThemeService;
        settingsServiceMock ??= mockSettingsService;

        settingsServiceMock
            .Setup(s => s.GetSettingAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Fluent11");
        settingsServiceMock
            .Setup(s => s.SaveSettingAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        themeServiceMock
            .Setup(t => t.GetAvailableThemes())
            .Returns(["Legacy95", "Fluent11", "Mica"]);
        themeServiceMock
            .Setup(t => t.CurrentTheme)
            .Returns("Fluent11");

        return new MainViewModel(
            processUseCaseMock.Object,
            filterUseCaseMock.Object,
            reevaluateMock.Object,
            exportDataUseCaseMock.Object,
            filePickerMock.Object,
            editDialogMock.Object,
            clipboardMock.Object,
            clipboardFormatterMock.Object,
            themeServiceMock.Object,
            settingsServiceMock.Object);
    }

    private static Mock<IFilterAssetsUseCase> BuildPassThroughFilterMock()
    {
        var mock = new Mock<IFilterAssetsUseCase>();
        mock.Setup(f => f.Execute(It.IsAny<IReadOnlyList<HardwareAsset>>(), It.IsAny<FilterCriteria>()))
            .Returns((IReadOnlyList<HardwareAsset> assets, FilterCriteria _) => assets);
        return mock;
    }

    [Fact]
    public async Task LoadFileAsync_PopulatesAssetsAndAppliesFilter_WhenSuccess()
    {
        var stream = new MemoryStream();
        var assets = new List<HardwareAsset> { BuildAsset("A001"), BuildAsset("A002") };

        var filePickerMock = new Mock<IFilePickerService>();
        filePickerMock
            .Setup(f => f.PickFileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        processUseCaseMock
            .Setup(u => u.ExecuteAsync(It.IsAny<Stream>(), It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);

        var viewModel = BuildViewModel(processUseCaseMock, exportDataUseCaseMock: mockExportUseCase, filePickerMock: filePickerMock);

        await viewModel.LoadFileCommand.ExecuteAsync(null);

        viewModel.Assets.Count.Should().Be(2);
        viewModel.IsLoading.Should().BeFalse();
        viewModel.FilteredAssets.Should().HaveCount(2);
    }

    [Fact]
    public async Task EditSelectedAssetAsync_ReplacesAssetAndReevaluates_WhenDialogConfirmed()
    {
        var original = BuildAsset("TEST001", SousEtat.Disponible);

        var editDialogMock = new Mock<IEditAssetDialogService>();
        editDialogMock
            .Setup(d => d.EditAsync(It.IsAny<SousEtat>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EditAssetDialogResult(SousEtat.Defectueux, "Test panne"));

        var reevaluateMock = new Mock<IReevaluateAssetUseCase>();
        reevaluateMock
            .Setup(r => r.Execute(It.IsAny<HardwareAsset>()))
            .Returns((HardwareAsset a) => a with { Evaluation = FakeEvaluation });

        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        var filterMock = BuildPassThroughFilterMock();
        var viewModel = BuildViewModel(
            processUseCaseMock,
            filterUseCaseMock: filterMock,
            reevaluateMock: reevaluateMock,
            exportDataUseCaseMock: mockExportUseCase,
            editDialogMock: editDialogMock);

        viewModel.Assets.Add(original);

        await viewModel.EditSelectedAssetCommand.ExecuteAsync(original);

        viewModel.Assets.Should().HaveCount(1);
        viewModel.Assets[0].SousEtat.Should().Be(SousEtat.Defectueux);
        viewModel.Assets[0].Evaluation.Should().NotBeNull();
        viewModel.FilteredAssets.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadFileAsync_ResetsLoadingState_WhenOperationCanceled()
    {
        var stream = new MemoryStream();

        var filePickerMock = new Mock<IFilePickerService>();
        filePickerMock
            .Setup(f => f.PickFileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        processUseCaseMock
            .Setup(u => u.ExecuteAsync(It.IsAny<Stream>(), It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var viewModel = BuildViewModel(processUseCaseMock, exportDataUseCaseMock: mockExportUseCase, filePickerMock: filePickerMock);

        Func<Task> act = () => viewModel.LoadFileCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync();
        viewModel.IsLoading.Should().BeFalse();
        viewModel.StatusMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ExportCsvCommand_CallsUseCase_WithFilteredAssets()
    {
        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        mockExportUseCase
            .Setup(x => x.ExecuteCsvExportAsync(It.IsAny<IReadOnlyList<HardwareAsset>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = BuildViewModel(processUseCaseMock, exportDataUseCaseMock: mockExportUseCase);
        viewModel.FilteredAssets =
        [
            BuildAsset("CSV001"),
            BuildAsset("CSV002")
        ];

        viewModel.ExportCsvCommand.Execute(null);

        mockExportUseCase.Verify(x => x.ExecuteCsvExportAsync(
            It.Is<IReadOnlyList<HardwareAsset>>(assets => assets.Count == 2 && assets[0].AssetTag == "CSV001" && assets[1].AssetTag == "CSV002"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ExportXlsxCommand_CallsUseCase_WithFilteredAssets()
    {
        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        mockExportUseCase
            .Setup(x => x.ExecuteXlsxExportAsync(It.IsAny<IReadOnlyList<HardwareAsset>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = BuildViewModel(processUseCaseMock, exportDataUseCaseMock: mockExportUseCase);
        viewModel.FilteredAssets =
        [
            BuildAsset("XLS001"),
            BuildAsset("XLS002")
        ];

        viewModel.ExportXlsxCommand.Execute(null);

        mockExportUseCase.Verify(x => x.ExecuteXlsxExportAsync(
            It.Is<IReadOnlyList<HardwareAsset>>(assets => assets.Count == 2 && assets[0].AssetTag == "XLS001" && assets[1].AssetTag == "XLS002"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CopySelectionToClipboardCommand_CallsFormatterAndClipboard()
    {
        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        var viewModel = BuildViewModel(processUseCaseMock);
        viewModel.FilteredAssets =
        [
            BuildAsset("CPY001"),
            BuildAsset("CPY002")
        ];

        mockClipboardFormatter
            .Setup(f => f.FormatAssets(It.IsAny<IReadOnlyList<HardwareAsset>>()))
            .Returns("formatted");

        viewModel.CopySelectionToClipboardCommand.Execute(null);

        mockClipboardFormatter.Verify(f => f.FormatAssets(
            It.Is<IReadOnlyList<HardwareAsset>>(assets => assets.Count == 2 && assets[0].AssetTag == "CPY001" && assets[1].AssetTag == "CPY002")),
            Times.Once);
        mockClipboardService.Verify(c => c.SetText("formatted"), Times.Once);
    }

    [Fact]
    public async Task ChangeThemeCommand_CallsThemeService_AndSavesSetting()
    {
        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        var viewModel = BuildViewModel(processUseCaseMock);
        viewModel.SelectedTheme = "Mica";

        await viewModel.ChangeThemeCommand.ExecuteAsync(null);

        mockThemeService.Verify(t => t.ApplyTheme("Mica"), Times.Once);
        mockSettingsService.Verify(s => s.SaveSettingAsync("Theme", "Mica"), Times.Once);
    }

    [Fact]
    public void CanCopySelection_ReturnsFalse_WhenFilteredAssetsEmpty_AndTrue_WhenNotEmpty()
    {
        var processUseCaseMock = new Mock<IProcessSpiceExportUseCase>();
        var viewModel = BuildViewModel(processUseCaseMock);

        viewModel.FilteredAssets = [];
        viewModel.CanCopySelection.Should().BeFalse();

        viewModel.FilteredAssets = [BuildAsset("CPY003")];
        viewModel.CanCopySelection.Should().BeTrue();
    }
}

