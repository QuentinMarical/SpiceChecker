using System.ComponentModel;
using System.Reflection;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.WinForms.ViewModels;

namespace SpiceChecker.WinForms.Views;

/// <summary>
/// Vue principale WinForms pilotée par le MainViewModel :
/// barre d'outils, filtres, tableau de résultats triable et barre de statut avec compteurs.
/// </summary>
public partial class MainForm : Form
{
    private readonly MainViewModel _viewModel;
    private readonly BindingSource _bindingSource = new();
    private readonly ContextMenuStrip _gridMenu = new();

    private Button _btnLoad = null!;
    private Button _btnExportCsv = null!;
    private Button _btnExportExcel = null!;
    private Button _btnCopy = null!;
    private Button _btnResetFilters = null!;
    private DataGridView _grid = null!;
    private const string CheckColumnName = "colCocher";

    private TextBox _txtSearch = null!;
    private ComboBox _cmbCategorie = null!;
    private ComboBox _cmbSousEtat = null!;
    private ComboBox _cmbSite = null!;
    private ComboBox _cmbResultat = null!;

    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripProgressBar _statusProgress = null!;
    private ToolStripStatusLabel _lblErreurs = null!;
    private ToolStripStatusLabel _lblAvertissements = null!;
    private ToolStripStatusLabel _lblInfos = null!;
    private ToolStripStatusLabel _lblConformes = null!;
    private ToolStripStatusLabel _lblAffiches = null!;

    private bool _isConfiguringGridColumns;
    private bool _gridColumnsConfigured;
    private bool _pendingGridColumnConfiguration;
    private readonly string? _startupFilePath;

    public MainForm(MainViewModel viewModel, string? startupFilePath = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _startupFilePath = startupFilePath;

        InitializeComponent();
        SetupBindings();
        WireEvents();
    }

    private void InitializeComponent()
    {
        Text = "SpiceChecker";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1000, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        KeyPreview = true;
        AllowDrop = true;

        // ── Barre d'outils ──────────────────────────────────────────────────
        var toolbarPanel = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(10, 10, 10, 4) };

        var toolbarLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _btnLoad = CreateToolbarButton("📂  Charger un export", 170, primary: true);
        _btnExportCsv = CreateToolbarButton("📄  Exporter CSV", 140);
        _btnExportExcel = CreateToolbarButton("📊  Exporter Excel", 145);
        _btnCopy = CreateToolbarButton("📋  Copier le tableau", 160);

        toolbarLeft.Controls.Add(_btnLoad);
        toolbarLeft.Controls.Add(_btnExportCsv);
        toolbarLeft.Controls.Add(_btnExportExcel);
        toolbarLeft.Controls.Add(_btnCopy);

        toolbarPanel.Controls.Add(toolbarLeft);

        // ── Barre de filtres ────────────────────────────────────────────────
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 46),
            Padding = new Padding(10, 6, 10, 4),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        _txtSearch = new TextBox
        {
            Name = "txtSearch",
            Width = 210,
            PlaceholderText = "Rechercher (étiquette, modèle, site…)",
            Margin = new Padding(0, 4, 12, 0)
        };

        _cmbCategorie = CreateFilterCombo("cmbCategorie", 130);
        _cmbSousEtat = CreateFilterCombo("cmbSousEtat", 185);
        _cmbSite = CreateFilterCombo("cmbSite", 145);
        _cmbResultat = CreateFilterCombo("cmbResultat", 165);

        _btnResetFilters = new Button
        {
            Name = "btnResetFilters",
            Text = "♻  Réinitialiser",
            Width = 125,
            Height = 28,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 3, 0, 0)
        };

        filterPanel.Controls.Add(CreateFilterLabel("Recherche"));
        filterPanel.Controls.Add(_txtSearch);
        filterPanel.Controls.Add(CreateFilterLabel("Catégorie"));
        filterPanel.Controls.Add(_cmbCategorie);
        filterPanel.Controls.Add(CreateFilterLabel("Sous-état"));
        filterPanel.Controls.Add(_cmbSousEtat);
        filterPanel.Controls.Add(CreateFilterLabel("Site"));
        filterPanel.Controls.Add(_cmbSite);
        filterPanel.Controls.Add(CreateFilterLabel("Résultat"));
        filterPanel.Controls.Add(_cmbResultat);
        filterPanel.Controls.Add(_btnResetFilters);

        // ── Tableau de résultats ────────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            // ReadOnly géré colonne par colonne : seule la colonne à cocher est éditable.
            ReadOnly = false,
            AutoGenerateColumns = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(230, 230, 230),
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 34,
            ShowCellToolTips = true,
            AllowDrop = true
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 243, 243);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(26, 26, 26);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f);
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
        _grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 228, 247);
        _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
        _grid.RowTemplate.Height = 28;
        EnableGridDoubleBuffering(_grid);

        _grid.DataBindingComplete += OnGridDataBindingComplete;
        _grid.CellFormatting += OnGridCellFormatting;
        _grid.CellToolTipTextNeeded += OnGridCellToolTipTextNeeded;
        _grid.ColumnHeaderMouseClick += OnGridColumnHeaderMouseClick;
        _grid.CellDoubleClick += OnGridCellDoubleClick;
        _grid.CurrentCellDirtyStateChanged += OnGridCurrentCellDirtyStateChanged;

        _gridMenu.Items.Add("✏  Modifier le sous-état", null, OnEditSubStateMenuClick);
        _gridMenu.Items.Add("🏷  Copier les étiquettes", null, OnCopyTagsMenuClick);
        _gridMenu.Items.Add("📋  Copier les lignes", null, OnCopyRowsMenuClick);
        _gridMenu.Items.Add(new ToolStripSeparator());
        _gridMenu.Items.Add("📄  Copier tout le tableau filtré", null, (_, _) => _viewModel.CopySelectionToClipboardCommand.Execute(null));
        _grid.ContextMenuStrip = _gridMenu;

        // ── Barre de statut ─────────────────────────────────────────────────
        _statusStrip = new StatusStrip { SizingGrip = false, Padding = new Padding(10, 3, 10, 3) };

        _statusLabel = new ToolStripStatusLabel("Prêt — glissez-déposez un export SPICE (.xlsx) ou utilisez Ctrl+O.")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusProgress = new ToolStripProgressBar { Width = 160, Minimum = 0, Maximum = 100, Visible = false };

        _lblErreurs = CreateCounterLabel(Color.Firebrick);
        _lblAvertissements = CreateCounterLabel(Color.DarkOrange);
        _lblInfos = CreateCounterLabel(Color.SteelBlue);
        _lblConformes = CreateCounterLabel(Color.SeaGreen);
        _lblAffiches = new ToolStripStatusLabel { Font = new Font("Segoe UI", 9f) };

        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_statusProgress);
        _statusStrip.Items.Add(_lblErreurs);
        _statusStrip.Items.Add(_lblAvertissements);
        _statusStrip.Items.Add(_lblInfos);
        _statusStrip.Items.Add(_lblConformes);
        _statusStrip.Items.Add(new ToolStripSeparator());
        _statusStrip.Items.Add(_lblAffiches);

        // L'ordre d'ajout définit le layout : Fill d'abord, puis les panneaux dockés.
        Controls.Add(_grid);
        Controls.Add(filterPanel);
        Controls.Add(toolbarPanel);
        Controls.Add(_statusStrip);

        UpdateCounters();
    }

    private static Button CreateToolbarButton(string text, int width, bool primary = false) => new()
    {
        Text = text,
        Width = width,
        Height = 32,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 8, 0),
        TextAlign = ContentAlignment.MiddleCenter,
        Tag = primary ? "primary" : null
    };

    private static Label CreateFilterLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 8, 6, 0)
    };

    private static ComboBox CreateFilterCombo(string name, int width) => new()
    {
        Name = name,
        Width = width,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Margin = new Padding(0, 4, 12, 0)
    };

    private static ToolStripStatusLabel CreateCounterLabel(Color color) => new()
    {
        ForeColor = color,
        Font = new Font("Segoe UI Semibold", 9f),
        Margin = new Padding(8, 3, 0, 2)
    };

    private static void EnableGridDoubleBuffering(DataGridView grid)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(grid, true);
    }

    private void SetupBindings()
    {
        _bindingSource.DataSource = _viewModel.FilteredAssets;
        _grid.DataSource = _bindingSource;

        _cmbCategorie.Items.Add("(toutes)");
        _cmbCategorie.Items.Add(new CategorieItem(CategorieEquipement.Ordinateur));
        _cmbCategorie.Items.Add(new CategorieItem(CategorieEquipement.EquipementReseau));
        _cmbCategorie.Items.Add(new CategorieItem(CategorieEquipement.Serveur));
        _cmbCategorie.SelectedIndex = 0;

        _cmbSousEtat.Items.Add("(tous)");
        foreach (var sousEtat in Enum.GetValues<SousEtat>())
        {
            _cmbSousEtat.Items.Add(new SousEtatItem(sousEtat));
        }

        _cmbSousEtat.SelectedIndex = 0;

        PopulateSiteCombo();

        _cmbResultat.Items.AddRange(
        [
            "(tous les résultats)",
            "Anomalies seulement",
            "Erreurs seulement",
            "Avertissements et plus",
            "Conformes seulement"
        ]);
        _cmbResultat.SelectedIndex = 0;

        _btnLoad.Enabled = !_viewModel.IsLoading;
        _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
    }

    private void WireEvents()
    {
        _btnLoad.Click += async (_, _) =>
        {
            _btnLoad.Enabled = false;
            try
            {
                await _viewModel.LoadFileCommand.ExecuteAsync(null);
            }
            finally
            {
                _btnLoad.Enabled = !_viewModel.IsLoading;
            }
        };

        _btnExportCsv.Click += async (_, _) => await _viewModel.ExportCsvCommand.ExecuteAsync(null);
        _btnExportExcel.Click += async (_, _) => await _viewModel.ExportXlsxCommand.ExecuteAsync(null);
        _btnCopy.Click += (_, _) => _viewModel.CopySelectionToClipboardCommand.Execute(null);
        _btnResetFilters.Click += (_, _) => ResetFilters();

        _txtSearch.TextChanged += (_, _) => _viewModel.SetSearchText(_txtSearch.Text);

        _cmbCategorie.SelectedIndexChanged += (_, _) =>
        {
            var value = _cmbCategorie.SelectedItem is CategorieItem item ? item.Valeur : (CategorieEquipement?)null;
            _viewModel.SetCategorie(value);
        };

        _cmbSousEtat.SelectedIndexChanged += (_, _) =>
        {
            var value = _cmbSousEtat.SelectedItem is SousEtatItem item ? item.Valeur : (SousEtat?)null;
            _viewModel.SetSousEtat(value);
        };

        _cmbSite.SelectedIndexChanged += (_, _) =>
        {
            var value = _cmbSite.SelectedIndex > 0 ? _cmbSite.SelectedItem as string : null;
            _viewModel.SetSite(value);
        };

        _cmbResultat.SelectedIndexChanged += (_, _) =>
        {
            switch (_cmbResultat.SelectedIndex)
            {
                case 1:
                    _viewModel.SetResultat(anomaliesOnly: true, niveauMin: null, conformesOnly: false);
                    break;
                case 2:
                    _viewModel.SetResultat(anomaliesOnly: false, niveauMin: NiveauAnomalie.Erreur, conformesOnly: false);
                    break;
                case 3:
                    _viewModel.SetResultat(anomaliesOnly: false, niveauMin: NiveauAnomalie.Avertissement, conformesOnly: false);
                    break;
                case 4:
                    _viewModel.SetResultat(anomaliesOnly: false, niveauMin: null, conformesOnly: true);
                    break;
                default:
                    _viewModel.SetResultat(anomaliesOnly: false, niveauMin: null, conformesOnly: false);
                    break;
            }
        };

        DragEnter += OnFileDragEnter;
        DragDrop += OnFileDragDrop;
        _grid.DragEnter += OnFileDragEnter;
        _grid.DragDrop += OnFileDragDrop;

        Shown += async (_, _) =>
        {
            TryConfigureGridColumns();

            await _viewModel.InitializeAsync();
            _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);

            TryConfigureGridColumns();

            if (_startupFilePath is not null)
            {
                await _viewModel.LoadFromPathAsync(_startupFilePath);
            }
        };

        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.O:
                _ = _viewModel.LoadFileCommand.ExecuteAsync(null);
                return true;

            case Keys.Control | Keys.S:
                _ = _viewModel.ExportCsvCommand.ExecuteAsync(null);
                return true;

            case Keys.Control | Keys.E:
                _ = _viewModel.ExportXlsxCommand.ExecuteAsync(null);
                return true;

            case Keys.Control | Keys.F:
                _txtSearch.Focus();
                _txtSearch.SelectAll();
                return true;

            case Keys.F5:
                _viewModel.ApplyFilterCommand.Execute(null);
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ResetFilters()
    {
        _txtSearch.Clear();
        _cmbCategorie.SelectedIndex = 0;
        _cmbSousEtat.SelectedIndex = 0;
        _cmbSite.SelectedIndex = 0;
        _cmbResultat.SelectedIndex = 0;
        _viewModel.ResetFilters();
        UpdateSortGlyphs();
    }

    private void PopulateSiteCombo()
    {
        var previousSelection = _cmbSite.SelectedIndex > 0 ? _cmbSite.SelectedItem as string : null;

        _cmbSite.BeginUpdate();
        _cmbSite.Items.Clear();
        _cmbSite.Items.Add("(tous)");
        foreach (var site in _viewModel.AvailableSites)
        {
            _cmbSite.Items.Add(site);
        }

        _cmbSite.SelectedIndex = previousSelection is not null && _cmbSite.Items.Contains(previousSelection)
            ? _cmbSite.Items.IndexOf(previousSelection)
            : 0;
        _cmbSite.EndUpdate();
    }

    private static void OnFileDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = TryGetDroppedXlsx(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnFileDragDrop(object? sender, DragEventArgs e)
    {
        var path = TryGetDroppedXlsx(e);
        if (path is not null)
        {
            await _viewModel.LoadFromPathAsync(path);
        }
    }

    private static string? TryGetDroppedXlsx(DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files
            && files.Length > 0
            && files[0].EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return files[0];
        }

        return null;
    }

    private async void OnEditSubStateMenuClick(object? sender, EventArgs e)
    {
        await EditCurrentRowAsync();
    }

    private async void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name != CheckColumnName)
        {
            await EditCurrentRowAsync();
        }
    }

    private void OnGridCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        // Valide immédiatement le clic sur une case à cocher.
        if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
        {
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private async Task EditCurrentRowAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not HardwareAsset asset)
        {
            return;
        }

        await _viewModel.EditSelectedAssetCommand.ExecuteAsync(asset);
    }

    /// <summary>
    /// Lignes visées par les actions de copie : les lignes cochées si au moins
    /// une case l'est, sinon les lignes sélectionnées (Ctrl/Maj + clic).
    /// </summary>
    private List<HardwareAsset> GetTargetAssets()
    {
        var checkedAssets = new List<HardwareAsset>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Cells[CheckColumnName].Value is true && row.DataBoundItem is HardwareAsset asset)
            {
                checkedAssets.Add(asset);
            }
        }

        if (checkedAssets.Count > 0)
        {
            return checkedAssets;
        }

        var selectedAssets = new List<HardwareAsset>();
        foreach (DataGridViewRow row in _grid.SelectedRows)
        {
            if (row.DataBoundItem is HardwareAsset asset)
            {
                selectedAssets.Add(asset);
            }
        }

        selectedAssets.Reverse();
        return selectedAssets;
    }

    private void OnCopyTagsMenuClick(object? sender, EventArgs e)
    {
        var assets = GetTargetAssets();
        if (assets.Count == 0)
        {
            _statusLabel.Text = "Aucune ligne cochée ou sélectionnée.";
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine, assets.Select(a => a.AssetTag)));
        _statusLabel.Text = $"{assets.Count} étiquette(s) copiée(s) dans le presse-papier.";
    }

    private void OnCopyRowsMenuClick(object? sender, EventArgs e)
    {
        var assets = GetTargetAssets();
        if (assets.Count == 0)
        {
            _statusLabel.Text = "Aucune ligne cochée ou sélectionnée.";
            return;
        }

        var lines = assets.Select(asset =>
        {
            var evaluation = asset.Evaluation is null
                ? "Conforme"
                : $"{asset.Evaluation.Niveau} — {asset.Evaluation.Message}";

            return string.Join('\t',
                asset.AssetTag,
                asset.Categorie.Libelle(),
                asset.Fabricant,
                asset.Modele,
                asset.RamGo?.ToString() ?? string.Empty,
                asset.SousEtat.Libelle(),
                asset.Entrepot,
                evaluation);
        });

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        _statusLabel.Text = $"{assets.Count} ligne(s) copiée(s) dans le presse-papier.";
    }

    private void OnGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0)
        {
            return;
        }

        // Clic sur l'en-tête de la colonne à cocher : tout cocher / tout décocher.
        if (_grid.Columns[e.ColumnIndex].Name == CheckColumnName)
        {
            ToggleAllCheckBoxes();
            return;
        }

        var propertyName = _grid.Columns[e.ColumnIndex].DataPropertyName;
        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        _viewModel.ToggleSort(propertyName);
    }

    private void ToggleAllCheckBoxes()
    {
        var anyUnchecked = false;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Cells[CheckColumnName].Value is not true)
            {
                anyUnchecked = true;
                break;
            }
        }

        _grid.EndEdit();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            row.Cells[CheckColumnName].Value = anyUnchecked;
        }

        _statusLabel.Text = anyUnchecked
            ? $"{_grid.Rows.Count} ligne(s) cochée(s)."
            : "Toutes les cases décochées.";
    }

    private void UpdateSortGlyphs()
    {
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.HeaderCell.SortGlyphDirection =
                column.DataPropertyName == _viewModel.SortProperty
                    ? (_viewModel.SortDescending ? SortOrder.Descending : SortOrder.Ascending)
                    : SortOrder.None;
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.FilteredAssets):
                RefreshBinding();
                UpdateCounters();
                _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
                break;

            case nameof(MainViewModel.CanCopySelection):
                _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
                break;

            case nameof(MainViewModel.AvailableSites):
                PopulateSiteCombo();
                break;

            case nameof(MainViewModel.StatusMessage):
                _statusLabel.Text = _viewModel.StatusMessage;
                break;

            case nameof(MainViewModel.ProgressPercentage):
                _statusProgress.Value = Math.Clamp(_viewModel.ProgressPercentage, 0, 100);
                break;

            case nameof(MainViewModel.IsLoading):
                _statusProgress.Visible = _viewModel.IsLoading;
                _btnLoad.Enabled = !_viewModel.IsLoading;
                break;
        }
    }

    private void UpdateCounters()
    {
        var assets = _viewModel.FilteredAssets;
        var erreurs = 0;
        var avertissements = 0;
        var infos = 0;
        var conformes = 0;

        foreach (var asset in assets)
        {
            switch (asset.Evaluation?.Niveau)
            {
                case NiveauAnomalie.Bloquant:
                case NiveauAnomalie.Erreur:
                    erreurs++;
                    break;
                case NiveauAnomalie.Avertissement:
                    avertissements++;
                    break;
                case NiveauAnomalie.Info:
                    infos++;
                    break;
                default:
                    conformes++;
                    break;
            }
        }

        _lblErreurs.Text = $"🔴 {erreurs}";
        _lblAvertissements.Text = $"🟠 {avertissements}";
        _lblInfos.Text = $"🔵 {infos}";
        _lblConformes.Text = $"✔ {conformes}";
        _lblAffiches.Text = $"{assets.Count} affiché(s) / {_viewModel.Assets.Count} au total";
    }

    private void RefreshBinding()
    {
        _gridColumnsConfigured = false;
        _pendingGridColumnConfiguration = true;
        _bindingSource.DataSource = _viewModel.FilteredAssets;
        _bindingSource.ResetBindings(false);
        TryConfigureGridColumns();
    }

    private void OnGridDataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        _pendingGridColumnConfiguration = true;
        TryConfigureGridColumns();
    }

    private void TryConfigureGridColumns()
    {
        if (_isConfiguringGridColumns || _gridColumnsConfigured || _grid.IsDisposed)
        {
            return;
        }

        if (_grid.Columns.Count == 0)
        {
            return;
        }

        if (!_pendingGridColumnConfiguration)
        {
            return;
        }

        if (!IsHandleCreated || !Visible)
        {
            return;
        }

        ConfigureGridColumns();
    }

    private void ConfigureGridColumns()
    {
        if (_grid.Columns.Count == 0)
        {
            return;
        }

        _isConfiguringGridColumns = true;
        _grid.SuspendLayout();

        try
        {
            EnsureCheckColumn();

            SetColumn(nameof(HardwareAsset.DateAcquisition), visible: false);
            SetColumn(nameof(HardwareAsset.Etat), visible: false);
            SetColumn(nameof(HardwareAsset.Emplacement), visible: false);

            var displayIndex = 1;
            SetColumn(nameof(HardwareAsset.AssetTag), "Étiquette", 95, ref displayIndex);
            SetColumn(nameof(HardwareAsset.Categorie), "Catégorie", 95, ref displayIndex);
            SetColumn(nameof(HardwareAsset.Fabricant), "Fabricant", 80, ref displayIndex);
            SetColumn(nameof(HardwareAsset.Modele), "Modèle", 195, ref displayIndex);
            SetColumn(nameof(HardwareAsset.RamGo), "RAM", 50, ref displayIndex);
            SetColumn(nameof(HardwareAsset.SousEtat), "Sous-état", 125, ref displayIndex);
            SetColumn(nameof(HardwareAsset.Entrepot), "Entrepôt", 85, ref displayIndex);
            SetColumn(nameof(HardwareAsset.DateRenouvellement), "Renouvel.", 85, ref displayIndex);
            SetColumn(nameof(HardwareAsset.DateDerniereModifSousEtat), "Depuis (j)", 70, ref displayIndex);
            SetColumn(nameof(HardwareAsset.Commentaire), "Commentaire", 110, ref displayIndex);
            SetColumn(nameof(HardwareAsset.Evaluation), "Résultat d'analyse", 320, ref displayIndex);

            if (_grid.Columns[nameof(HardwareAsset.DateRenouvellement)] is { } dateRenouvellementColumn)
            {
                dateRenouvellementColumn.DefaultCellStyle.Format = "dd/MM/yyyy";
            }

            if (_grid.Columns[nameof(HardwareAsset.Evaluation)] is { } evaluationColumn)
            {
                evaluationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                evaluationColumn.MinimumWidth = 220;
            }

            // Seule la colonne à cocher est éditable.
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.ReadOnly = column.Name != CheckColumnName;
            }

            UpdateSortGlyphs();

            _gridColumnsConfigured = true;
            _pendingGridColumnConfiguration = false;
        }
        finally
        {
            _grid.ResumeLayout();
            _isConfiguringGridColumns = false;
        }
    }

    private void EnsureCheckColumn()
    {
        if (_grid.Columns[CheckColumnName] is { } existing)
        {
            existing.DisplayIndex = 0;
            return;
        }

        var checkColumn = new DataGridViewCheckBoxColumn
        {
            Name = CheckColumnName,
            HeaderText = "☑",
            Width = 36,
            MinimumWidth = 36,
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            FalseValue = false,
            TrueValue = true
        };
        checkColumn.HeaderCell.ToolTipText = "Cocher des lignes puis clic droit pour copier les étiquettes ou les lignes. Clic sur l'en-tête : tout cocher/décocher.";

        _grid.Columns.Insert(0, checkColumn);
        checkColumn.DisplayIndex = 0;
    }

    private void SetColumn(string name, bool visible)
    {
        if (_grid.Columns[name] is { } column)
        {
            column.Visible = visible;
        }
    }

    private void SetColumn(string name, string headerText, int width, ref int displayIndex)
    {
        if (_grid.Columns[name] is not { } column)
        {
            return;
        }

        column.Visible = true;
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        column.SortMode = DataGridViewColumnSortMode.Programmatic;
        column.HeaderText = headerText;
        column.DisplayIndex = displayIndex;
        column.Width = width;
        displayIndex++;
    }

    private static void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (grid.Rows[e.RowIndex].DataBoundItem is HardwareAsset rowAsset)
        {
            var backColor = rowAsset.Evaluation?.Niveau switch
            {
                NiveauAnomalie.Bloquant or NiveauAnomalie.Erreur => Color.FromArgb(255, 224, 224),
                NiveauAnomalie.Avertissement => Color.FromArgb(255, 240, 214),
                NiveauAnomalie.Info => Color.FromArgb(222, 237, 255),
                _ => (Color?)null
            };

            if (backColor.HasValue)
            {
                e.CellStyle!.BackColor = backColor.Value;
                e.CellStyle.ForeColor = Color.Black;
            }
        }

        var propertyName = grid.Columns[e.ColumnIndex].DataPropertyName;

        switch (propertyName)
        {
            case nameof(HardwareAsset.SousEtat):
                if (e.Value is SousEtat sousEtat)
                {
                    e.Value = sousEtat.Libelle();
                    e.FormattingApplied = true;
                }

                break;

            case nameof(HardwareAsset.Categorie):
                if (e.Value is CategorieEquipement categorie)
                {
                    e.Value = categorie.Libelle();
                    e.FormattingApplied = true;
                }

                break;

            case nameof(HardwareAsset.DateDerniereModifSousEtat):
                if (e.Value is DateTime date)
                {
                    e.Value = Math.Max(0, (DateTime.Today - date.Date).Days).ToString();
                    e.FormattingApplied = true;
                }

                break;

            case nameof(HardwareAsset.Evaluation):
                if (e.Value is EvaluationResult evaluation)
                {
                    var prefix = evaluation.Niveau switch
                    {
                        NiveauAnomalie.Bloquant => "⛔ Bloquant — ",
                        NiveauAnomalie.Erreur => "🔴 Erreur — ",
                        NiveauAnomalie.Avertissement => "🟠 Avertissement — ",
                        NiveauAnomalie.Info => "🔵 Info — ",
                        _ => string.Empty
                    };

                    e.Value = prefix + evaluation.Message;
                }
                else
                {
                    e.Value = "✔ Conforme";
                }

                e.FormattingApplied = true;
                break;
        }
    }

    private void OnGridCellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not HardwareAsset asset)
        {
            return;
        }

        var lines = new List<string>
        {
            $"{asset.AssetTag} — {asset.Fabricant} {asset.Modele}",
            $"Sous-état : {asset.SousEtat.Libelle()}"
        };

        if (!string.IsNullOrWhiteSpace(asset.Emplacement))
        {
            lines.Add($"Emplacement : {asset.Emplacement}");
        }

        if (asset.Evaluation is { } evaluation)
        {
            lines.Add($"Règle : {evaluation.RegleDeclenchee} ({evaluation.Niveau})");
            lines.Add(evaluation.Message);
        }
        else
        {
            lines.Add("Aucune anomalie détectée.");
        }

        if (!string.IsNullOrWhiteSpace(asset.Commentaire))
        {
            lines.Add($"Commentaire : {asset.Commentaire}");
        }

        lines.Add(string.Empty);
        lines.Add("Double-clic : modifier le sous-état");

        e.ToolTipText = string.Join(Environment.NewLine, lines);
    }

    private sealed record SousEtatItem(SousEtat Valeur)
    {
        public override string ToString() => Valeur.Libelle();
    }

    private sealed record CategorieItem(CategorieEquipement Valeur)
    {
        public override string ToString() => Valeur.Libelle();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _bindingSource.Dispose();
            _gridMenu.Dispose();
        }

        base.Dispose(disposing);
    }
}
