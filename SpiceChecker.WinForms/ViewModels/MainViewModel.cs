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

    /// <summary>
    /// Sites (entrepôts / emplacements) présents dans le fichier chargé.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<string> AvailableSites { get; set; } = new();

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = string.Empty;

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        if (IsLoading)
        {
            return;
        }

        StatusMessage = "Sélection du fichier...";
        var stream = await _filePickerService.PickFileAsync();
        if (stream is null)
        {
            StatusMessage = "Chargement annulé.";
            return;
        }

        await ImportStreamAsync(stream);
    }

    /// <summary>
    /// Charge un export SPICE depuis un chemin (glisser-déposer d'un fichier .xlsx).
    /// </summary>
    public async Task LoadFromPathAsync(string path)
    {
        if (IsLoading || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        await ImportStreamAsync(File.OpenRead(path));
    }

    private async Task ImportStreamAsync(Stream stream)
    {
        try
        {
            IsLoading = true;
            ProgressPercentage = 0;

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
            RefreshAvailableSites();
            ApplyFilter();

            // Laisse passer les derniers rapports de progression en file d'attente
            // avant d'afficher le message final (sinon ils l'écrasent).
            await Task.Yield();

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
            await stream.DisposeAsync();
            IsLoading = false;
        }
    }

    /// <summary>
    /// Propriété de tri courante (nom de propriété de <see cref="HardwareAsset"/>), ou null.
    /// </summary>
    public string? SortProperty { get; private set; }

    /// <summary>
    /// Sens du tri courant.
    /// </summary>
    public bool SortDescending { get; private set; }

    /// <summary>
    /// Bascule le tri sur la propriété donnée : croissant, puis décroissant, puis aucun.
    /// </summary>
    public void ToggleSort(string propertyName)
    {
        if (SortProperty == propertyName)
        {
            if (SortDescending)
            {
                SortProperty = null;
                SortDescending = false;
            }
            else
            {
                SortDescending = true;
            }
        }
        else
        {
            SortProperty = propertyName;
            SortDescending = false;
        }

        ApplyFilter();
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        IReadOnlyList<HardwareAsset> filtered = _filterAssetsUseCase.Execute(Assets, CurrentFilter);

        if (SortProperty is not null)
        {
            var sorted = filtered.ToList();
            sorted.Sort(BuildComparison(SortProperty));
            if (SortDescending)
            {
                sorted.Reverse();
            }

            filtered = sorted;
        }

        FilteredAssets = new ObservableCollection<HardwareAsset>(filtered);
        OnPropertyChanged(nameof(CanCopySelection));
        CopySelectionToClipboardCommand.NotifyCanExecuteChanged();
    }

    private static Comparison<HardwareAsset> BuildComparison(string propertyName) => propertyName switch
    {
        nameof(HardwareAsset.AssetTag) => (a, b) => string.Compare(a.AssetTag, b.AssetTag, StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.Categorie) => (a, b) => string.Compare(a.Categorie.Libelle(), b.Categorie.Libelle(), StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.Fabricant) => (a, b) => string.Compare(a.Fabricant, b.Fabricant, StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.Modele) => (a, b) => string.Compare(a.Modele, b.Modele, StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.RamGo) => (a, b) => Nullable.Compare(a.RamGo, b.RamGo),
        nameof(HardwareAsset.SousEtat) => (a, b) => string.Compare(a.SousEtat.Libelle(), b.SousEtat.Libelle(), StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.Entrepot) => (a, b) => string.Compare(a.Entrepot, b.Entrepot, StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.DateRenouvellement) => (a, b) => Nullable.Compare(a.DateRenouvellement, b.DateRenouvellement),
        nameof(HardwareAsset.DateDerniereModifSousEtat) => (a, b) => Nullable.Compare(a.DateDerniereModifSousEtat, b.DateDerniereModifSousEtat),
        nameof(HardwareAsset.Commentaire) => (a, b) => string.Compare(a.Commentaire, b.Commentaire, StringComparison.OrdinalIgnoreCase),
        nameof(HardwareAsset.Evaluation) => (a, b) => GetSeverityRankForSort(a).CompareTo(GetSeverityRankForSort(b)),
        _ => (_, _) => 0
    };

    private static int GetSeverityRankForSort(HardwareAsset asset) => asset.Evaluation?.Niveau switch
    {
        NiveauAnomalie.Bloquant => 4,
        NiveauAnomalie.Erreur => 3,
        NiveauAnomalie.Avertissement => 2,
        NiveauAnomalie.Info => 1,
        _ => 0
    };

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

    public void SetSousEtat(SousEtat? sousEtat)
    {
        CurrentFilter = CurrentFilter with { SousEtat = sousEtat };
        ApplyFilter();
    }

    public void SetSite(string? site)
    {
        CurrentFilter = CurrentFilter with { Site = site };
        ApplyFilter();
    }

    /// <summary>
    /// Reconstruit la liste des sites à partir des entrepôts et emplacements du fichier chargé.
    /// </summary>
    private void RefreshAvailableSites()
    {
        var sites = Assets
            .SelectMany(a => new[] { a.Entrepot, a.Emplacement })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);

        AvailableSites = new ObservableCollection<string>(sites);
    }

    /// <summary>
    /// Applique le filtre de résultat d'analyse (tous / anomalies / niveau minimal / conformes).
    /// </summary>
    public void SetResultat(bool anomaliesOnly, NiveauAnomalie? niveauMin, bool conformesOnly)
    {
        CurrentFilter = CurrentFilter with
        {
            AnomaliesOnly = anomaliesOnly,
            NiveauMin = niveauMin,
            ConformesOnly = conformesOnly
        };
        ApplyFilter();
    }

    /// <summary>
    /// Réinitialise tous les critères de filtrage et le tri.
    /// </summary>
    public void ResetFilters()
    {
        CurrentFilter = new FilterCriteria();
        SortProperty = null;
        SortDescending = false;
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
