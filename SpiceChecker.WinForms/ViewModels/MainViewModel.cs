using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpiceChecker.Application.Services;
using SpiceChecker.Application.UseCases;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.WinForms.ViewModels;

/// <summary>
/// ViewModel principal de l'interface WinForms.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IProcessSpiceExportUseCase _processSpiceExportUseCase;
    private readonly IFilterAssetsUseCase _filterAssetsUseCase;
    private readonly IReevaluateAssetUseCase _reevaluateAssetUseCase;
    private readonly IExportDataUseCase _exportDataUseCase;
    private readonly IFilePickerService _filePickerService;
    private readonly IEditAssetDialogService _editAssetDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IClipboardExportFormatter _clipboardExportFormatter;
    private readonly IThemeService _themeService;
    private readonly ISettingsService _settingsService;

    public MainViewModel(
        IProcessSpiceExportUseCase processSpiceExportUseCase,
        IFilterAssetsUseCase filterAssetsUseCase,
        IReevaluateAssetUseCase reevaluateAssetUseCase,
        IExportDataUseCase exportDataUseCase,
        IFilePickerService filePickerService,
        IEditAssetDialogService editAssetDialogService,
        IClipboardService clipboardService,
        IClipboardExportFormatter clipboardExportFormatter,
        IThemeService themeService,
        ISettingsService settingsService)
    {
        _processSpiceExportUseCase = processSpiceExportUseCase ?? throw new ArgumentNullException(nameof(processSpiceExportUseCase));
        _filterAssetsUseCase = filterAssetsUseCase ?? throw new ArgumentNullException(nameof(filterAssetsUseCase));
        _reevaluateAssetUseCase = reevaluateAssetUseCase ?? throw new ArgumentNullException(nameof(reevaluateAssetUseCase));
        _exportDataUseCase = exportDataUseCase ?? throw new ArgumentNullException(nameof(exportDataUseCase));
        _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
        _editAssetDialogService = editAssetDialogService ?? throw new ArgumentNullException(nameof(editAssetDialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _clipboardExportFormatter = clipboardExportFormatter ?? throw new ArgumentNullException(nameof(clipboardExportFormatter));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        Assets = new();
        FilteredAssets = new();
        CurrentFilter = new();
        StatusMessage = "Prêt";

        var themes = _themeService.GetAvailableThemes() ?? [];
        AvailableThemes = new ObservableCollection<string>(themes);
        SelectedTheme = string.IsNullOrWhiteSpace(_themeService.CurrentTheme)
            ? "Fluent11"
            : _themeService.CurrentTheme;
    }

    [ObservableProperty]
    public partial ObservableCollection<HardwareAsset> Assets { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCopySelection))]
    [NotifyCanExecuteChangedFor(nameof(CopySelectionToClipboardCommand))]
    public partial ObservableCollection<HardwareAsset> FilteredAssets { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ProgressPercentage { get; set; }

    [ObservableProperty]
    public partial FilterCriteria CurrentFilter { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableThemes { get; set; }

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = string.Empty;

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        if (IsLoading)
        {
            return;
        }

        Stream? stream = null;

        try
        {
            IsLoading = true;
            ProgressPercentage = 0;
            StatusMessage = "Sélection du fichier...";

            stream = await _filePickerService.PickFileAsync();
            if (stream is null)
            {
                StatusMessage = "Chargement annulé.";
                return;
            }

            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
                if (TryExtractProgress(message, out var current, out var total) && total > 0)
                {
                    ProgressPercentage = (int)Math.Clamp((current * 100.0) / total, 0, 100);
                }
            });

            var evaluatedAssets = await _processSpiceExportUseCase.ExecuteAsync(stream, progress, CancellationToken.None);

            Assets = new ObservableCollection<HardwareAsset>(evaluatedAssets);
            ApplyFilter();

            ProgressPercentage = 100;
            StatusMessage = $"Chargement terminé ({Assets.Count} éléments).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Chargement annulé.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            if (stream is not null)
            {
                await stream.DisposeAsync();
            }

            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var filtered = _filterAssetsUseCase.Execute(Assets, CurrentFilter);
        FilteredAssets = new ObservableCollection<HardwareAsset>(filtered);
        OnPropertyChanged(nameof(CanCopySelection));
        CopySelectionToClipboardCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task EditSelectedAssetAsync(HardwareAsset? asset)
    {
        if (asset is null)
        {
            return;
        }

        var dialogResult = await _editAssetDialogService.EditAsync(asset.SousEtat, asset.Commentaire, CancellationToken.None);
        if (dialogResult is null)
        {
            return;
        }

        var updatedAsset = asset with
        {
            SousEtat = dialogResult.SousEtat,
            Commentaire = dialogResult.Commentaire,
            DateDerniereModifSousEtat = DateTime.Today
        };

        var finalAsset = _reevaluateAssetUseCase.Execute(updatedAsset);

        var index = Assets.IndexOf(asset);
        if (index >= 0)
        {
            Assets[index] = finalAsset;
        }
        else
        {
            Assets.Add(finalAsset);
        }

        ApplyFilter();
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        await ExecuteExportAsync(exportCsv: true);
    }

    [RelayCommand]
    private async Task ExportXlsxAsync()
    {
        await ExecuteExportAsync(exportCsv: false);
    }

    public bool CanCopySelection => FilteredAssets?.Count > 0;

    [RelayCommand(CanExecute = nameof(CanCopySelection))]
    private void CopySelectionToClipboard()
    {
        var assetsToCopy = FilteredAssets.ToList();
        if (assetsToCopy.Count == 0)
        {
            StatusMessage = "0 lignes copiées dans le presse-papier.";
            return;
        }

        var text = _clipboardExportFormatter.FormatAssets(assetsToCopy);
        _clipboardService.SetText(text);
        StatusMessage = $"{assetsToCopy.Count} lignes copiées dans le presse-papier.";
    }

    [RelayCommand]
    private async Task ChangeThemeAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedTheme))
        {
            return;
        }

        _themeService.ApplyTheme(SelectedTheme);
        await _settingsService.SaveSettingAsync("Theme", SelectedTheme);
        StatusMessage = $"Thème appliqué : {SelectedTheme}.";
    }

    public async Task InitializeAsync()
    {
        var savedTheme = await _settingsService.GetSettingAsync("Theme", "Fluent11");
        if (!string.IsNullOrWhiteSpace(savedTheme) && AvailableThemes.Contains(savedTheme))
        {
            SelectedTheme = savedTheme;
        }
        else
        {
            SelectedTheme = "Fluent11";
        }

        await ChangeThemeAsync();
    }

    private async Task ExecuteExportAsync(bool exportCsv)
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = exportCsv ? "Export CSV en cours..." : "Export Excel en cours...";

            if (exportCsv)
            {
                await _exportDataUseCase.ExecuteCsvExportAsync([.. FilteredAssets], CancellationToken.None);
            }
            else
            {
                await _exportDataUseCase.ExecuteXlsxExportAsync([.. FilteredAssets], CancellationToken.None);
            }

            StatusMessage = exportCsv ? "Export CSV terminé." : "Export Excel terminé.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetSearchText(string? searchText)
    {
        CurrentFilter = CurrentFilter with { SearchText = searchText };
        ApplyFilter();
    }

    public void SetCategorie(CategorieEquipement? categorie)
    {
        CurrentFilter = CurrentFilter with { Categorie = categorie };
        ApplyFilter();
    }

    public void SetAnomaliesOnly(bool anomaliesOnly)
    {
        CurrentFilter = CurrentFilter with { AnomaliesOnly = anomaliesOnly };
        ApplyFilter();
    }

    private static bool TryExtractProgress(string message, out int current, out int total)
    {
        current = 0;
        total = 0;

        var marker = "Évaluation :";
        if (string.IsNullOrWhiteSpace(message) || !message.Contains(marker, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var span = message.AsSpan();
        var markerIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var valuesSpan = span[(markerIndex + marker.Length)..].Trim();
        var slashIndex = valuesSpan.IndexOf('/');

        if (slashIndex < 0)
        {
            return false;
        }

        var currentSpan = valuesSpan[..slashIndex].Trim();
        var totalSpan = valuesSpan[(slashIndex + 1)..].Trim();

        return int.TryParse(currentSpan, out current)
               && int.TryParse(totalSpan, out total);
    }
}
