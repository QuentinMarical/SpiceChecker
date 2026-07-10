using System.ComponentModel;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.WinForms.ViewModels;

namespace SpiceChecker.WinForms.Views;

/// <summary>
/// Vue minimale WinForms pilotée par le MainViewModel.
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
    private Button _btnAppliquerFiltre = null!;
    private DataGridView _grid = null!;
    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbCategorie = null!;
    private CheckBox _chkAnomaliesOnly = null!;
    private ComboBox _cmbTheme = null!;

    public MainForm(MainViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        SetupBindings();
        WireEvents();
    }

    private void InitializeComponent()
    {
        Text = "SpiceChecker v2";
        Width = 1200;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        _btnLoad = new Button
        {
            Name = "btnLoad",
            Text = "Charger",
            Left = 12,
            Top = 12,
            Width = 100,
            Height = 30
        };

        _progressBar = new ProgressBar
        {
            Left = 130,
            Top = 17,
            Width = 280,
            Height = 20,
            Minimum = 0,
            Maximum = 100
        };

        _lblStatus = new Label
        {
            Left = 430,
            Top = 20,
            Width = 730,
            Height = 20,
            AutoEllipsis = true,
            Text = "Prêt"
        };

        var lblSearch = new Label { Left = 12, Top = 58, Width = 70, Text = "Recherche" };
        _txtSearch = new TextBox { Name = "txtSearch", Left = 82, Top = 54, Width = 260 };

        var lblCategorie = new Label { Left = 356, Top = 58, Width = 70, Text = "Catégorie" };
        _cmbCategorie = new ComboBox
        {
            Name = "cmbCategorie",
            Left = 426,
            Top = 54,
            Width = 190,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _btnExportCsv = new Button
        {
            Name = "btnExportCsv",
            Text = "Exporter CSV",
            Left = 880,
            Top = 12,
            Width = 110,
            Height = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _btnExportExcel = new Button
        {
            Name = "btnExportExcel",
            Text = "Exporter Excel",
            Left = 996,
            Top = 12,
            Width = 120,
            Height = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _btnCopy = new Button
        {
            Name = "btnCopy",
            Text = "Copier",
            Left = 996,
            Top = 52,
            Width = 120,
            Height = 26,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _chkAnomaliesOnly = new CheckBox
        {
            Name = "chkAnomaliesOnly",
            Left = 632,
            Top = 57,
            Width = 135,
            Height = 22,
            Text = "Anomalies seules"
        };

        var lblTheme = new Label { Left = 776, Top = 58, Width = 52, Text = "Thème" };
        _cmbTheme = new ComboBox
        {
            Name = "cmbTheme",
            Left = 828,
            Top = 54,
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _btnAppliquerFiltre = new Button
        {
            Name = "btnAppliquerFiltre",
            Text = "Appliquer",
            Left = 880,
            Top = 52,
            Width = 90,
            Height = 26,
            Visible = false
        };

        _grid = new DataGridView
        {
            Left = 12,
            Top = 90,
            Width = 1160,
            Height = 620,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _grid.DataBindingComplete += (_, _) => ConfigureGridColumns();
        _grid.CellFormatting += OnGridCellFormatting;

        _gridMenu.Items.Add("Modifier le sous-état", null, OnEditSubStateMenuClick);
        _gridMenu.Items.Add("Copier", null, (_, _) => _viewModel.CopySelectionToClipboardCommand.Execute(null));
        _grid.ContextMenuStrip = _gridMenu;

        Controls.Add(_btnLoad);
        Controls.Add(_btnExportCsv);
        Controls.Add(_btnExportExcel);
        Controls.Add(_btnCopy);
        Controls.Add(_progressBar);
        Controls.Add(_lblStatus);
        Controls.Add(lblSearch);
        Controls.Add(_txtSearch);
        Controls.Add(lblCategorie);
        Controls.Add(_cmbCategorie);
        Controls.Add(_chkAnomaliesOnly);
        Controls.Add(lblTheme);
        Controls.Add(_cmbTheme);
        Controls.Add(_btnAppliquerFiltre);
        Controls.Add(_grid);
    }

    private void SetupBindings()
    {
        _bindingSource.DataSource = _viewModel.FilteredAssets;
        _grid.DataSource = _bindingSource;

        _lblStatus.DataBindings.Add(nameof(Label.Text), _viewModel, nameof(MainViewModel.StatusMessage), false, DataSourceUpdateMode.OnPropertyChanged);
        _progressBar.DataBindings.Add(nameof(ProgressBar.Value), _viewModel, nameof(MainViewModel.ProgressPercentage), false, DataSourceUpdateMode.OnPropertyChanged);

        _cmbCategorie.Items.Add("(toutes)");
        _cmbCategorie.Items.Add(CategorieEquipement.Ordinateur);
        _cmbCategorie.Items.Add(CategorieEquipement.EquipementReseau);
        _cmbCategorie.Items.Add(CategorieEquipement.Serveur);
        _cmbCategorie.SelectedIndex = 0;

        _cmbTheme.DataSource = _viewModel.AvailableThemes;
        if (!string.IsNullOrWhiteSpace(_viewModel.SelectedTheme))
        {
            _cmbTheme.SelectedItem = _viewModel.SelectedTheme;
        }

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

        _txtSearch.TextChanged += (_, _) =>
        {
            _viewModel.SetSearchText(_txtSearch.Text);
            _viewModel.ApplyFilterCommand.Execute(null);
        };

        _cmbCategorie.SelectedIndexChanged += (_, _) =>
        {
            var value = _cmbCategorie.SelectedItem is CategorieEquipement c ? c : (CategorieEquipement?)null;
            _viewModel.SetCategorie(value);
            _viewModel.ApplyFilterCommand.Execute(null);
        };

        _chkAnomaliesOnly.CheckedChanged += (_, _) =>
        {
            _viewModel.SetAnomaliesOnly(_chkAnomaliesOnly.Checked);
            _viewModel.ApplyFilterCommand.Execute(null);
        };

        _btnAppliquerFiltre.Click += (_, _) => _viewModel.ApplyFilterCommand.Execute(null);
        _cmbTheme.SelectedIndexChanged += async (_, _) =>
        {
            if (_cmbTheme.SelectedItem is string theme)
            {
                _viewModel.SelectedTheme = theme;
                await _viewModel.ChangeThemeCommand.ExecuteAsync(null);
                _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
            }
        };

        Shown += async (_, _) =>
        {
            await _viewModel.InitializeAsync();
            _cmbTheme.SelectedItem = _viewModel.SelectedTheme;
            _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
        };

        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private async void OnEditSubStateMenuClick(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is not HardwareAsset asset)
        {
            return;
        }

        await _viewModel.EditSelectedAssetCommand.ExecuteAsync(asset);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.FilteredAssets))
        {
            RefreshBinding();
            _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
        }
        else if (e.PropertyName == nameof(MainViewModel.CanCopySelection))
        {
            _btnCopy.Enabled = _viewModel.CopySelectionToClipboardCommand.CanExecute(null);
        }
    }

    private void RefreshBinding()
    {
        _bindingSource.DataSource = _viewModel.FilteredAssets;
        _bindingSource.ResetBindings(false);
    }

    private void ConfigureGridColumns()
    {
        if (_grid.Columns.Count == 0)
        {
            return;
        }

        // Colonnes techniques masquées.
        SetColumn(nameof(HardwareAsset.DateAcquisition), visible: false);
        SetColumn(nameof(HardwareAsset.Etat), visible: false);

        // Colonnes visibles : libellé, largeur et ordre d'affichage.
        var displayIndex = 0;
        SetColumn(nameof(HardwareAsset.AssetTag), "Étiquette", 100, ref displayIndex);
        SetColumn(nameof(HardwareAsset.Categorie), "Catégorie", 110, ref displayIndex);
        SetColumn(nameof(HardwareAsset.Fabricant), "Fabricant", 85, ref displayIndex);
        SetColumn(nameof(HardwareAsset.Modele), "Modèle", 220, ref displayIndex);
        SetColumn(nameof(HardwareAsset.RamGo), "RAM (Go)", 65, ref displayIndex);
        SetColumn(nameof(HardwareAsset.SousEtat), "Sous-état", 140, ref displayIndex);
        SetColumn(nameof(HardwareAsset.Entrepot), "Entrepôt", 90, ref displayIndex);
        SetColumn(nameof(HardwareAsset.DateRenouvellement), "Renouvellement", 100, ref displayIndex);
        SetColumn(nameof(HardwareAsset.DateDerniereModifSousEtat), "Sous-état depuis (j)", 105, ref displayIndex);
        SetColumn(nameof(HardwareAsset.Commentaire), "Commentaire", 140, ref displayIndex);
        SetColumn(nameof(HardwareAsset.Evaluation), "Résultat d'analyse", 320, ref displayIndex);

        if (_grid.Columns[nameof(HardwareAsset.DateRenouvellement)] is { } dateRenouvellementColumn)
        {
            dateRenouvellementColumn.DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        if (_grid.Columns[nameof(HardwareAsset.Evaluation)] is { } evaluationColumn)
        {
            evaluationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            evaluationColumn.MinimumWidth = 260;
        }
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

        column.HeaderText = headerText;
        column.Width = width;
        column.DisplayIndex = displayIndex;
        displayIndex++;
    }

    private static void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0)
        {
            return;
        }

        // Teinte de fond de toute la ligne selon la sévérité de l'anomalie.
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
