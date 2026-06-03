using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ClosedXML.Excel;
using SpiceChecker.Forms;
using SpiceChecker.Models;
using SpiceChecker.Rules;
using SpiceChecker.Services;
using ThemeDefinition = SpiceChecker.Models.AppTheme;
using WinFormsAppTheme = SpiceChecker.Services.AppTheme;

namespace SpiceChecker
{
    public partial class MainForm : Form
    {
        private readonly XlsxLoader _loader = new XlsxLoader();
        private readonly RuleEngine _engine = new RuleEngine();

        private List<HardwareRow> _allRows = new List<HardwareRow>();
        private BindingList<HardwareRow> _view = new BindingList<HardwareRow>();
        private readonly BindingSource _bs = new BindingSource();

        // Toolbar
        private Panel _toolbarPanel = null!;
        private ToolStrip _toolbar = null!;
        private ToolStripButton _btnOpen = null!, _btnExport = null!, _btnExportXlsx = null!, _btnPrint = null!, _btnCopy = null!, _btnAbout = null!;
        private ToolStripLabel _lblSource = null!, _lblTheme = null!;
        private ToolStripComboBox _cbTheme = null!;
        private readonly SettingsService.AppSettings _settings = SettingsService.Load();
        private ThemeDefinition _currentTheme = ThemeCatalog.GetTheme(ThemeId.Fluent11Dark);
        private CustomTitleBar _titleBar = null!;

        // Filtres
        private Panel _filterPanelHost = null!;
        private TableLayoutPanel _filterPanel = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cbSousEtat = null!, _cbCategorie = null!, _cbFabricant = null!, _cbModele = null!;
        private CheckBox _chkAnomaliesOnly = null!;
        private Label _lblCount = null!;

        // Grille + statut
        private readonly Panel _titleBarPanel = new Panel();
        public Panel TitleBarPanel => _titleBarPanel;
        private DataGridView _grid = null!;
        private StatusStrip _status = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStripStatusLabel _lblCountErreur = null!, _lblCountAvert = null!, _lblCountOk = null!;
        private ContextMenuStrip _contextMenu = null!;
        private int _contextMenuRowIndex = -1;

        // Pour l'impression
        private int _printRowIndex = 0;

        private const int WM_GETMINMAXINFO = 0x0024;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public System.Drawing.Point ptReserved;
            public System.Drawing.Point ptMaxSize;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Point ptMinTrackSize;
            public System.Drawing.Point ptMaxTrackSize;
        }

        public MainForm()
        {
            InitializeComponent();
            ConfigureMainForm();
            BuildUi();
            WireEvents();
            UpdateCount();
            InitializeTitleBarAndTheme();
        }

        private void ConfigureMainForm()
        {
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 760);
            Name = "MainForm";
            Text = "Spice Checker — Analyse stock SPICE";
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;
            MinimumSize = new Size(960, 540);
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
        }

        private void BuildUi()
        {
            // Toolbar
            _toolbarPanel = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(0), BackColor = Color.Transparent };
            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Fill };
            _btnOpen = new ToolStripButton("Ouvrir XLSX...");
            _btnExport = new ToolStripButton("Export CSV") { Enabled = false };
            _btnExportXlsx = new ToolStripButton("Export XLSX") { Enabled = false };
            _btnPrint = new ToolStripButton("Imprimer") { Enabled = false };
            _btnCopy = new ToolStripButton("Copier sélection") { Enabled = false };
            _btnAbout = new ToolStripButton("À propos");
            _lblSource = new ToolStripLabel("Aucun fichier chargé") { ForeColor = Color.Gray };
            _lblTheme = new ToolStripLabel("Thème :") { Alignment = ToolStripItemAlignment.Right };
            _cbTheme = new ToolStripComboBox
            {
                Alignment = ToolStripItemAlignment.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130
            };
            _cbTheme.Items.AddRange(new object[]
            {
                "Legacy 95",
                "Luna XP",
                "Aero 7",
                "Modern (clair)",
                "Modern (sombre)",
                "Fluent 11 (clair)",
                "Fluent 11 (sombre)"
            });

            try
            {
                _btnOpen.Image = new Bitmap(SystemIcons.Application.ToBitmap(), new Size(16, 16));
                _btnOpen.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                _btnOpen.ImageScaling = ToolStripItemImageScaling.None;
            }
            catch { }

            try
            {
                _btnExport.Image = new Bitmap(SystemIcons.Shield.ToBitmap(), new Size(16, 16));
                _btnExport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                _btnExport.ImageScaling = ToolStripItemImageScaling.None;
            }
            catch { }

            try
            {
                Bitmap printImage;
                try
                {
                    printImage = new Bitmap(typeof(PrintDocument), "print.bmp");
                    printImage = new Bitmap(printImage, new Size(16, 16));
                }
                catch
                {
                    printImage = new Bitmap(16, 16);
                    using var g = Graphics.FromImage(printImage);
                    g.Clear(Color.White);
                }

                _btnPrint.Image = printImage;
                _btnPrint.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                _btnPrint.ImageScaling = ToolStripItemImageScaling.None;
            }
            catch { }

            try
            {
                _btnAbout.Image = new Bitmap(SystemIcons.Information.ToBitmap(), new Size(16, 16));
                _btnAbout.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                _btnAbout.ImageScaling = ToolStripItemImageScaling.None;
            }
            catch { }

            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                _btnOpen, new ToolStripSeparator(),
                _btnExport, _btnExportXlsx, _btnPrint, _btnCopy,
                new ToolStripSeparator(), _btnAbout,
                new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right }, _cbTheme, _lblTheme,
                new ToolStripSeparator(), _lblSource
            });
            _toolbarPanel.Controls.Add(_toolbar);

            // Filtres
            _filterPanelHost = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            _filterPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6, 4, 6, 4),
                RowCount = 1,
                ColumnCount = 13,
                BackColor = Color.Transparent
            };

            for (int i = 0; i < 13; i++)
            {
                _filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }
            _filterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblSearch = new Label { Text = "Recherche :", AutoSize = true, Anchor = AnchorStyles.Left };
            _txtSearch = new TextBox
            {
                AutoSize = false,
                Width = 260,
                PlaceholderText = "Tag, modèle, NS, utilisateur...",
                Anchor = AnchorStyles.Left
            };

            var lblSE = new Label { Text = "Sous-état :", AutoSize = true, Anchor = AnchorStyles.Left };
            _cbSousEtat = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left };

            var lblCat = new Label { Text = "Catégorie :", AutoSize = true, Anchor = AnchorStyles.Left };
            _cbCategorie = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left };

            var lblFab = new Label { Text = "Fabricant :", AutoSize = true, Anchor = AnchorStyles.Left };
            _cbFabricant = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left };

            var lblMod = new Label { Text = "Modèle :", AutoSize = true, Anchor = AnchorStyles.Left };
            _cbModele = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left };

            _chkAnomaliesOnly = new CheckBox { Text = "Anomalies uniquement", AutoSize = true, Anchor = AnchorStyles.Left, BackColor = Color.Transparent, FlatStyle = FlatStyle.System };
            var btnReset = new Button { Text = "Réinitialiser", AutoSize = true, Anchor = AnchorStyles.Left, FlatStyle = FlatStyle.System };
            btnReset.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                if (_cbSousEtat.Items.Count > 0) _cbSousEtat.SelectedIndex = 0;
                if (_cbCategorie.Items.Count > 0) _cbCategorie.SelectedIndex = 0;
                if (_cbFabricant.Items.Count > 0) _cbFabricant.SelectedIndex = 0;
                RebuildModeleDropdown();
                if (_cbModele.Items.Count > 0) _cbModele.SelectedIndex = 0;
                _chkAnomaliesOnly.Checked = false;
                ApplyFilters();
            };

            _lblCount = new Label { Text = "0 / 0", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Anchor = AnchorStyles.Left };

            _filterPanel.Controls.Add(lblSearch, 0, 0);
            _filterPanel.Controls.Add(_txtSearch, 1, 0);
            _filterPanel.Controls.Add(lblSE, 2, 0);
            _filterPanel.Controls.Add(_cbSousEtat, 3, 0);
            _filterPanel.Controls.Add(lblCat, 4, 0);
            _filterPanel.Controls.Add(_cbCategorie, 5, 0);
            _filterPanel.Controls.Add(lblFab, 6, 0);
            _filterPanel.Controls.Add(_cbFabricant, 7, 0);
            _filterPanel.Controls.Add(lblMod, 8, 0);
            _filterPanel.Controls.Add(_cbModele, 9, 0);
            _filterPanel.Controls.Add(_chkAnomaliesOnly, 10, 0);
            _filterPanel.Controls.Add(btnReset, 11, 0);
            _filterPanel.Controls.Add(_lblCount, 12, 0);

            // Grille
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };
            _grid.DefaultCellStyle.BackColor = Color.FromArgb(0, 0, 0, 0);
            ConfigureColumns();
            _bs.DataSource = _view;
            _grid.DataSource = _bs;

            // Menu contextuel (initialise à null, sera défini dynamiquement au clic droit)
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Modifier le sous-état...", null, (s, e) => EditSubState());
            _grid.ContextMenuStrip = null; // Désactiver par défaut

            // Status
            _status = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Prêt — glisse un fichier XLSX ou clique Ouvrir");
            _lblCountErreur = new ToolStripStatusLabel("🔴 Erreurs : 0") { ForeColor = Color.DarkRed };
            _lblCountAvert = new ToolStripStatusLabel("🟠 Avert. : 0") { ForeColor = Color.DarkOrange };
            _lblCountOk = new ToolStripStatusLabel("🟢 OK : 0") { ForeColor = Color.DarkGreen };
            _status.Items.Add(_statusLabel);
            _status.Items.Add(new ToolStripSeparator());
            _status.Items.Add(_lblCountErreur);
            _status.Items.Add(new ToolStripSeparator());
            _status.Items.Add(_lblCountAvert);
            _status.Items.Add(new ToolStripSeparator());
            _status.Items.Add(_lblCountOk);

            _filterPanelHost.Controls.Add(_filterPanel);

            _toolbarPanel.Paint += (s, e) =>
            {
                using var p = new Pen(_currentTheme.BorderColor);
                e.Graphics.DrawLine(p, 0, _toolbarPanel.Height - 1, _toolbarPanel.Width, _toolbarPanel.Height - 1);
            };
            _filterPanelHost.Paint += (s, e) =>
            {
                using var p = new Pen(_currentTheme.BorderColor);
                e.Graphics.DrawLine(p, 0, _filterPanelHost.Height - 1, _filterPanelHost.Width, _filterPanelHost.Height - 1);
            };

            Controls.Add(_grid);
            Controls.Add(_filterPanelHost);
            Controls.Add(_toolbarPanel);
            Controls.Add(_status);
        }

        private void InitializeTitleBarAndTheme()
        {
            if (!Enum.TryParse<ThemeId>(_settings.LastTheme, true, out var savedThemeId))
            {
                savedThemeId = _settings.LastTheme switch
                {
                    nameof(WinFormsAppTheme.Legacy95) => ThemeId.Legacy95,
                    nameof(WinFormsAppTheme.LunaXP) => ThemeId.LunaXP,
                    nameof(WinFormsAppTheme.AeroSeven) => ThemeId.Aero7,
                    nameof(WinFormsAppTheme.ModernLight) => ThemeId.ModernLight,
                    nameof(WinFormsAppTheme.ModernDark) => ThemeId.ModernDark,
                    nameof(WinFormsAppTheme.FluentLight) => ThemeId.Fluent11Light,
                    nameof(WinFormsAppTheme.FluentDark) => ThemeId.Fluent11Dark,
                    _ => ThemeId.Fluent11Dark
                };
            }

            _currentTheme = ThemeCatalog.GetTheme(savedThemeId);
            EnsureCustomTitleBar();

            if (_cbTheme.SelectedIndex != (int)_currentTheme.Id)
            {
                _cbTheme.SelectedIndex = (int)_currentTheme.Id;
            }

            ApplyCurrentTheme();
        }

        private void EnsureCustomTitleBar()
        {
            if (_titleBar == null)
            {
                _titleBar = new CustomTitleBar();
                _titleBar.Initialize(this, _currentTheme, "Spice Checker — Analyse stock SPICE");
                _titleBar.CloseClicked += (s, _) => Application.Exit();
                _titleBar.MinimizeClicked += (s, _) => WindowState = FormWindowState.Minimized;
                _titleBar.MaximizeClicked += (s, _) =>
                    WindowState = WindowState == FormWindowState.Maximized
                        ? FormWindowState.Normal
                        : FormWindowState.Maximized;
            }

            if (!Controls.Contains(_titleBar))
            {
                Controls.Add(_titleBar);
            }

            _titleBar.Visible = true;
            _titleBar.Dock = DockStyle.Top;
        }

        private void EnsureTopDockOrder()
        {
            SuspendLayout();
            try
            {
                if (Controls.Contains(_filterPanelHost)) Controls.SetChildIndex(_filterPanelHost, Controls.Count - 1);
                if (Controls.Contains(_toolbarPanel)) Controls.SetChildIndex(_toolbarPanel, Controls.Count - 1);
                if (Controls.Contains(_titleBar)) Controls.SetChildIndex(_titleBar, Controls.Count - 1);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void ApplyCurrentTheme()
        {
            bool isFluent = _currentTheme.Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark;

            EnsureCustomTitleBar();
            _titleBar.ApplyTheme(_currentTheme);
            ThemeApplier.Apply(this, _currentTheme, _grid, _toolbarPanel, _filterPanelHost, _toolbar);

            Padding = _currentTheme.HasOuterBorder3D
                ? new Padding(2)
                : new Padding(1);

            EnsureTopDockOrder();
            ApplyFluentEffect();
            Invalidate();
        }

        private void ApplyFluentEffect()
        {
            if (!IsHandleCreated) return;

            bool isFluent = _currentTheme.Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark;

            if (isFluent)
            {
                // Rétablir la title bar native Windows
                FormBorderStyle = FormBorderStyle.Sizable;

                // Masquer CustomTitleBar
                if (_titleBar != null) _titleBar.Visible = false;

                // Dark mode sur la title bar native
                DwmHelper.SetDarkTitleBar(Handle, _currentTheme.Id == ThemeId.Fluent11Dark);

                // Mica > Mica legacy > Acrylic legacy
                DwmHelper.ApplyBestEffect(Handle, BackdropEffect.Mica, _currentTheme.BackdropFallbackTint);
            }
            else
            {
                // Retour à CustomTitleBar pour les autres thèmes
                FormBorderStyle = FormBorderStyle.None;
                if (_titleBar != null) _titleBar.Visible = true;

                // Appliquer backdrop pour Aero7 / ModernDark
                if (_currentTheme.Backdrop != BackdropEffect.None)
                {
                    DwmHelper.SetDarkTitleBar(Handle, _currentTheme.IsDark);
                    DwmHelper.ApplyBestEffect(Handle, _currentTheme.Backdrop, _currentTheme.BackdropFallbackTint);
                }
                else
                {
                    DwmHelper.DisableBackdrop(Handle);
                }
            }
        }

        private void ConfigureColumns()
        {
            _grid.Columns.Clear();

            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selectionnee",
                HeaderText = "",
                Width = 36,
                Name = "col_Selectionnee",
                ReadOnly = false,
                Frozen = true
            });

            void Add(string prop, string header, int w)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = prop,
                    HeaderText = header,
                    Width = w,
                    Name = "col_" + prop,
                    ReadOnly = true
                });
            }
            Add("AssetTag", "Étiquette", 110);
            Add("SousEtat", "Sous-état", 170);
            Add("CategorieModele", "Catégorie", 130);
            Add("Fabricant", "Fabricant", 100);
            Add("Modele", "Modèle", 200);
            Add("RamGo", "RAM (Go)", 70);
            Add("AffecteA", "Affecté à", 150);
            Add("AnomalieNiveau", "Niveau", 90);
            Add("AnomalieMessage", "Anomalie", 280);
            Add("SousEtatConseille", "Conseil", 150);

            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.ColumnHeadersHeight = 30;
        }
        // ==================================================================
        //  Événements
        // ==================================================================
        private void WireEvents()
        {
            _btnOpen.Click += (s, e) => OpenFileDialogAndLoad();
            _btnExport.Click += (s, e) => ExportCsv();
            _btnExportXlsx.Click += (s, e) => ExportXlsx();
            _btnPrint.Click += (s, e) => PrintGrid();
            _btnCopy.Click += (s, e) => CopySelection();
            _btnAbout.Click += (s, e) => MessageBox.Show(
                "Spice Checker\nAnalyse XLSX SPICE / ServiceNow\n\nMARICAL Quentin — 2026",
                "À propos", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            _cbSousEtat.SelectedIndexChanged += (s, e) => ApplyFilters();
            _cbCategorie.SelectedIndexChanged += (s, e) => ApplyFilters();
            _cbFabricant.SelectedIndexChanged += (s, e) =>
            {
                RebuildModeleDropdown();
                ApplyFilters();
            };
            _cbModele.SelectedIndexChanged += (s, e) => ApplyFilters();
            _chkAnomaliesOnly.CheckedChanged += (s, e) => ApplyFilters();
            _cbTheme.SelectedIndexChanged += (s, e) =>
            {
                if (_cbTheme.SelectedIndex < 0)
                {
                    return;
                }

                _currentTheme = ThemeCatalog.GetTheme((ThemeId)_cbTheme.SelectedIndex);
                ApplyCurrentTheme();
                _settings.LastTheme = _currentTheme.Id.ToString();
                SettingsService.Save(_settings);
            };

            _grid.CellFormatting += Grid_CellFormatting;
            _grid.CellMouseDown += Grid_CellMouseDown;
            _grid.CellContentClick += Grid_CellContentClick;
            _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;

            // Drag & drop
            this.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    // Accepter si au moins un fichier .xlsx est présent
                    var hasXlsx = files?.Any(f => f != null && f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) ?? false;
                    e.Effect = hasXlsx ? DragDropEffects.Copy : DragDropEffects.None;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };
            this.DragDrop += (s, e) =>
            {
                if (e.Data == null) return;
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data == null) return;
                var xlsx = data.FirstOrDefault(f => f != null && f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
                if (xlsx != null) LoadFile(xlsx);
            };

            // Raccourcis clavier
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.O) { OpenFileDialogAndLoad(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.F) { _txtSearch.Focus(); _txtSearch.SelectAll(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.E && _btnExport.Enabled) { ExportCsv(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.X && _btnExportXlsx.Enabled) { ExportXlsx(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.P && _btnPrint.Enabled) { PrintGrid(); e.Handled = true; }
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DwmHelper.ApplyModernBackdrop(Handle, DwmHelper.BackdropType.Mica);
            ApplyFluentEffect();
        }

        // ==================================================================
        //  Chargement XLSX
        // ==================================================================
        private void OpenFileDialogAndLoad()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Tous les fichiers (*.*)|*.*",
                Title = "Ouvrir un export SPICE"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                LoadFile(dlg.FileName);
        }

        private void LoadFile(string path)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                _statusLabel.Text = "Chargement de " + Path.GetFileName(path) + "...";
                Application.DoEvents();

                var res = _loader.Load(path);
                _engine.EvaluateAll(res.Rows);
                _allRows = res.Rows;

                RebuildFilterDropdowns();
                ApplyFilters();

                _lblSource.Text = $"{Path.GetFileName(path)}  ({_allRows.Count} lignes, feuille \"{res.SheetName}\")";
                _lblSource.ForeColor = Color.Black;
                _btnExport.Enabled = _btnPrint.Enabled = _btnCopy.Enabled = _btnExportXlsx.Enabled = _allRows.Count > 0;

                var warn = res.Warnings.Count > 0 ? "  ⚠ " + string.Join(" | ", res.Warnings) : "";
                _statusLabel.Text = $"Chargé : {_allRows.Count} lignes." + warn;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur de chargement :\n\n" + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "Erreur de chargement.";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // ==================================================================
        //  Filtres
        // ==================================================================
        private void RebuildFilterDropdowns()
        {
            var sousEtats = _allRows.Select(r => r.SousEtat ?? "")
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Distinct().OrderBy(s => s).ToList();
            var categories = _allRows.Select(r => r.CategorieModele ?? "")
                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                     .Distinct().OrderBy(s => s).ToList();
            var fabricants = _allRows.Select(r => r.Fabricant ?? "")
                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                     .Distinct().OrderBy(s => s).ToList();

            _cbSousEtat.BeginUpdate();
            _cbSousEtat.Items.Clear();
            _cbSousEtat.Items.Add("(tous)");
            foreach (var s in sousEtats) _cbSousEtat.Items.Add(s);
            _cbSousEtat.SelectedIndex = 0;
            _cbSousEtat.EndUpdate();

            _cbCategorie.BeginUpdate();
            _cbCategorie.Items.Clear();
            _cbCategorie.Items.Add("(toutes)");
            foreach (var c in categories) _cbCategorie.Items.Add(c);
            _cbCategorie.SelectedIndex = 0;
            _cbCategorie.EndUpdate();

            _cbFabricant.BeginUpdate();
            _cbFabricant.Items.Clear();
            _cbFabricant.Items.Add("(tous)");
            foreach (var f in fabricants) _cbFabricant.Items.Add(f);
            _cbFabricant.SelectedIndex = 0;
            _cbFabricant.EndUpdate();

            RebuildModeleDropdown();
            if (_cbModele.Items.Count > 0)
                _cbModele.SelectedIndex = 0;
        }

        private void RebuildModeleDropdown()
        {
            var fabSel = _cbFabricant.SelectedItem as string;
            IEnumerable<HardwareRow> q = _allRows;

            if (!string.IsNullOrEmpty(fabSel) && fabSel != "(tous)")
                q = q.Where(r => string.Equals(r.Fabricant, fabSel, StringComparison.OrdinalIgnoreCase));

            var modeles = q.Select(r => r.Modele ?? "")
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .Distinct().OrderBy(s => s).ToList();

            var selectedModele = _cbModele.SelectedItem as string;

            _cbModele.BeginUpdate();
            _cbModele.Items.Clear();
            _cbModele.Items.Add("(tous)");
            foreach (var m in modeles) _cbModele.Items.Add(m);

            if (!string.IsNullOrEmpty(selectedModele) && _cbModele.Items.Contains(selectedModele))
                _cbModele.SelectedItem = selectedModele;
            else
                _cbModele.SelectedIndex = 0;

            _cbModele.EndUpdate();
        }

        private void ApplyFilters()
        {
            var search = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();
            var seSel = _cbSousEtat.SelectedItem as string;
            var catSel = _cbCategorie.SelectedItem as string;
            var fabSel = _cbFabricant.SelectedItem as string;
            var modSel = _cbModele.SelectedItem as string;
            bool onlyAno = _chkAnomaliesOnly.Checked;

            IEnumerable<HardwareRow> q = _allRows;

            if (!string.IsNullOrEmpty(seSel) && seSel != "(tous)")
                q = q.Where(r => string.Equals(r.SousEtat, seSel, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(catSel) && catSel != "(toutes)")
                q = q.Where(r => string.Equals(r.CategorieModele, catSel, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(fabSel) && fabSel != "(tous)")
                q = q.Where(r => string.Equals(r.Fabricant, fabSel, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(modSel) && modSel != "(tous)")
                q = q.Where(r => string.Equals(r.Modele, modSel, StringComparison.OrdinalIgnoreCase));

            if (onlyAno)
                q = q.Where(r => !string.IsNullOrEmpty(r.AnomalieNiveau) && r.AnomalieNiveau != "OK");

            if (search.Length > 0)
            {
                q = q.Where(r =>
                    (r.AssetTag ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Modele ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Fabricant ?? "").ToLowerInvariant().Contains(search) ||
                    (r.AffecteA ?? "").ToLowerInvariant().Contains(search) ||
                    (r.SousEtat ?? "").ToLowerInvariant().Contains(search));
            }

            var list = q.ToList();
            _view = new BindingList<HardwareRow>(list);
            _bs.DataSource = _view;
            UpdateCount();
        }

        private void UpdateCount()
        {
            _lblCount.Text = $"{_view.Count} / {_allRows.Count}";

            var nbErreurs = _view.Count(r => string.Equals(r.AnomalieNiveau, "Erreur", StringComparison.OrdinalIgnoreCase));
            var nbAvertissements = _view.Count(r => string.Equals(r.AnomalieNiveau, "Avertissement", StringComparison.OrdinalIgnoreCase));
            var nbOk = _view.Count(r => string.Equals(r.AnomalieNiveau, "OK", StringComparison.OrdinalIgnoreCase));

            _lblCountErreur.Text = $"🔴 Erreurs : {nbErreurs}";
            _lblCountAvert.Text = $"🟠 Avert. : {nbAvertissements}";
            _lblCountOk.Text = $"🟢 OK : {nbOk}";
        }

        // ==================================================================
        //  Coloration des lignes selon le niveau d'anomalie
        // ==================================================================
        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _view.Count) return;
            var row = _view[e.RowIndex];
            if (row == null) return;

            Color back;
            switch (row.AnomalieNiveau)
            {
                case "Erreur": back = Color.FromArgb(255, 200, 200); break;
                case "Avertissement": back = Color.FromArgb(255, 220, 180); break;
                case "Info": back = Color.FromArgb(200, 220, 255); break;
                case "OK": back = Color.FromArgb(220, 245, 220); break;
                default: back = (e.RowIndex % 2 == 1) ? Color.FromArgb(247, 247, 247) : SystemColors.Window; break;
            }
            e.CellStyle.BackColor = back;

            var columnName = _grid.Columns[e.ColumnIndex].Name;
            if (columnName == "col_AnomalieNiveau")
            {
                e.Value = row.AnomalieNiveau switch
                {
                    "Erreur" => "🔴 Erreur",
                    "Avertissement" => "🟠 Avert.",
                    "Info" => "🔵 Info",
                    "OK" => "🟢 OK",
                    _ => "—"
                };
                e.FormattingApplied = true;
            }

            if (row.AnomalieNiveau == "Erreur" && (columnName == "col_AnomalieNiveau" || columnName == "col_AnomalieMessage"))
            {
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            else
            {
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
        }

        // ==================================================================
        //  Menu contextuel pour modifier le sous-état
        // ==================================================================
        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) 
            {
                _contextMenuRowIndex = -1;
                _grid.ContextMenuStrip = null; // Ne pas afficher le menu si pas de ligne
                return;
            }

            if (e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "col_Selectionnee")
            {
                _contextMenuRowIndex = -1;
                _grid.ContextMenuStrip = null;
                return;
            }

            // Sélectionner la ligne cliquée et mémoriser l'index
            _contextMenuRowIndex = e.RowIndex;
            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;
            _grid.ContextMenuStrip = _contextMenu; // Autoriser le menu contextuel
        }

        private void Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "col_Selectionnee") return;

            _grid.EndEdit();
            _grid.InvalidateRow(e.RowIndex);
        }

        private void EditSubState()
        {
            if (_contextMenuRowIndex < 0 || _contextMenuRowIndex >= _view.Count)
                return;

            var row = _view[_contextMenuRowIndex];
            if (row == null)
                return;

            // Afficher le formulaire de dialogue pour modifier le sous-état
            using (var dlg = new EditSubStateForm(row.SousEtat ?? ""))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var newSubState = dlg.SelectedSubState.Trim();
                    if (!string.IsNullOrEmpty(newSubState) && newSubState != row.SousEtat)
                    {
                        // Mettre à jour le sous-état
                        row.SousEtat = newSubState;

                        // Réévaluer la ligne
                        _engine.EvaluateRow(row);

                        // Rafraîchir l'affichage du DataGridView
                        // _view est une BindingList qui référence les mêmes objets que _allRows
                        // donc les modifications sont automatiquement propagées
                        _grid.Invalidate();

                        _statusLabel.Text = $"Sous-état modifié pour {row.AssetTag} : {newSubState}";
                    }
                }
            }

            _contextMenuRowIndex = -1;
        }

        // ==================================================================
        //  Export CSV UTF-8 BOM
        // ==================================================================
        private void ExportCsv()
        {
            if (_view.Count == 0) return;

            using var dlg = new SaveFileDialog
            {
                Filter = "CSV (séparé par ;) (*.csv)|*.csv",
                Title = "Exporter en CSV",
                FileName = "spice_export_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                var cols = _grid.Columns.Cast<DataGridViewColumn>()
                            .Where(c => c.Visible && c.Name != "col_Selectionnee").OrderBy(c => c.DisplayIndex).ToList();

                sb.AppendLine(string.Join(";", cols.Select(c => CsvEscape(c.HeaderText))));

                foreach (var row in _view)
                {
                    var values = cols.Select(c => CsvEscape(GetCellString(row, c.DataPropertyName)));
                    sb.AppendLine(string.Join(";", values));
                }

                // UTF-8 avec BOM (Excel friendly)
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                _statusLabel.Text = "Export CSV : " + Path.GetFileName(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur d'export :\n" + ex.Message,
                    "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================================================================
        //  Export XLSX avec ClosedXML (anomalies colonnes dédiées)
        // ==================================================================
        private void ExportXlsx()
        {
            if (_view.Count == 0) return;

            using var dlg = new SaveFileDialog
            {
                Filter = "Classeur Excel (*.xlsx)|*.xlsx",
                Title = "Exporter en XLSX",
                FileName = "spice_anomalies_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                _statusLabel.Text = "Génération du XLSX...";
                Application.DoEvents();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Spice_Anomalies");

                var cols = _grid.Columns.Cast<DataGridViewColumn>()
                            .Where(c => c.Visible && c.Name != "col_Selectionnee").OrderBy(c => c.DisplayIndex).ToList();

                int col = 1;
                foreach (var c in cols)
                {
                    var cell = ws.Cell(1, col);
                    cell.Value = c.HeaderText;
                    cell.Style.Font.SetBold();
                    cell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(230, 230, 230));
                    col++;
                }

                string[] extraHeaders = { "⚠ Anomalie ?", "Niveau", "Action Recommandée" };
                foreach (var h in extraHeaders)
                {
                    var cell = ws.Cell(1, col);
                    cell.Value = h;
                    cell.Style.Font.SetBold();
                    cell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 200, 200));
                    col++;
                }

                int row = 2;
                foreach (var hwRow in _view)
                {
                    col = 1;
                    foreach (var c in cols)
                    {
                        ws.Cell(row, col).Value = GetCellString(hwRow, c.DataPropertyName);
                        col++;
                    }

                    bool isAnomaly = !string.IsNullOrEmpty(hwRow.AnomalieNiveau) && hwRow.AnomalieNiveau != "OK";
                    ws.Cell(row, col++).Value = isAnomaly ? "OUI" : "Non";
                    ws.Cell(row, col++).Value = hwRow.AnomalieNiveau ?? "N/A";
                    ws.Cell(row, col++).Value = hwRow.SousEtatConseille ?? "N/A";

                    var range = ws.Range(row, 1, row, col - 1);
                    switch (hwRow.AnomalieNiveau)
                    {
                        case "Erreur":
                            range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 200, 200));
                            break;
                        case "Avertissement":
                            range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 220, 180));
                            break;
                        case "Info":
                            range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(200, 220, 255));
                            break;
                        case "OK":
                            range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(220, 245, 220));
                            break;
                    }

                    row++;
                }

                ws.Columns().AdjustToContents();
                ws.Row(1).Height = 22;
                ws.SheetView.FreezeRows(1);
                ws.Range(1, 1, Math.Max(1, row - 1), Math.Max(1, col - 1)).SetAutoFilter();

                wb.SaveAs(dlg.FileName);

                _statusLabel.Text = "Export XLSX : " + Path.GetFileName(dlg.FileName) +
                                    $" ({_view.Count} lignes)";
                MessageBox.Show(this,
                    $"Export terminé !\n\n{_view.Count} lignes exportées.\n\n" +
                    "Les colonnes \"⚠ Anomalie ?\", \"Niveau\" et \"Action Recommandée\"\n" +
                    "permettent de trier et filtrer directement dans Excel.",
                    "Export XLSX", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Erreur d'export XLSX :\n" + ex.Message,
                    "Export XLSX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private static string CsvEscape(string s)
        {
            if (s == null) return "";
            bool quote = s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            s = s.Replace("\"", "\"\"");
            return quote ? "\"" + s + "\"" : s;
        }

        private static string GetCellString(HardwareRow row, string prop)
        {
            var p = typeof(HardwareRow).GetProperty(prop);
            if (p == null) return "";
            var v = p.GetValue(row);
            if (v == null) return "";
            if (v is DateTime dt) return dt.ToString("yyyy-MM-dd");
            return v.ToString() ?? "";
        }

        // ==================================================================
        //  Copier la sélection (TSV pour Excel)
        // ==================================================================
        private void CopySelection()
        {
            if (_grid.SelectedRows.Count == 0) return;

            var cols = _grid.Columns.Cast<DataGridViewColumn>()
                        .Where(c => c.Visible && c.Name != "col_Selectionnee").OrderBy(c => c.DisplayIndex).ToList();

            var sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", cols.Select(c => c.HeaderText)));

            foreach (DataGridViewRow r in _grid.SelectedRows.Cast<DataGridViewRow>()
                                         .OrderBy(r => r.Index))
            {
                if (r.DataBoundItem is HardwareRow hw)
                {
                    var values = cols.Select(c => GetCellString(hw, c.DataPropertyName)
                                                  .Replace("\t", " ").Replace("\r", " ").Replace("\n", " "));
                    sb.AppendLine(string.Join("\t", values));
                }
            }

            Clipboard.SetText(sb.ToString());
            _statusLabel.Text = $"{_grid.SelectedRows.Count} ligne(s) copiée(s) dans le presse-papiers.";
        }

        // ==================================================================
        //  Impression A4 paysage
        // ==================================================================
        private void PrintGrid()
        {
            if (_view.Count == 0) return;

            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            pd.DefaultPageSettings.Margins = new Margins(40, 40, 50, 40);
            _printRowIndex = 0;
            pd.PrintPage += Pd_PrintPage;

            using var dlg = new PrintPreviewDialog
            {
                Document = pd,
                Width = 1100,
                Height = 750,
                StartPosition = FormStartPosition.CenterParent
            };
            dlg.ShowDialog(this);
        }

        private void Pd_PrintPage(object? sender, PrintPageEventArgs e)
        {
            if (_view.Count == 0)
            {
                e.HasMorePages = false;
                return;
            }

            var g = e.Graphics;
            if (g is null)
            {
                e.HasMorePages = false;
                return;
            }

            var pageRect = g.VisibleClipBounds;
            var bounds = new RectangleF(40, 40, pageRect.Width - 80, pageRect.Height - 80);

            using var font = new Font("Segoe UI", 8f);
            using var bold = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var title = new Font("Segoe UI", 12f, FontStyle.Bold);

            string header = "Spice Checker — " + _lblSource.Text +
                            "    (" + _view.Count + " lignes filtrées)    " +
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            g.DrawString(header, title, Brushes.Black, bounds.Left, bounds.Top);

            float y = bounds.Top + 28;

            var printCols = new (string Prop, string Header, float W)[]
            {
                ("AssetTag",         "Étiquette", 80),
                ("SousEtat",         "Sous-état", 110),
                ("CategorieModele",  "Catégorie", 90),
                ("Fabricant",        "Fabricant", 70),
                ("Modele",           "Modèle",    140),
                ("RamGo",            "RAM",       40),
                ("AnomalieNiveau",   "Niveau",    60),
                ("AnomalieMessage",  "Anomalie",  180)
            };

            float x = bounds.Left;
            using (var headerBrush = new SolidBrush(Color.FromArgb(230, 230, 230)))
            {
                g.FillRectangle(headerBrush, bounds.Left, y, bounds.Width, 20);
            }

            foreach (var col in printCols)
            {
                g.DrawString(col.Header, bold, Brushes.Black, x + 2, y + 3);
                x += col.W;
            }
            y += 22;

            float rowHeight = 18f;
            using var format = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            while (_printRowIndex < _view.Count && y + rowHeight < bounds.Bottom - 10)
            {
                var row = _view[_printRowIndex];

                Color back = row.AnomalieNiveau switch
                {
                    "Erreur" => Color.FromArgb(255, 200, 200),
                    "Avertissement" => Color.FromArgb(255, 220, 180),
                    "Info" => Color.FromArgb(200, 220, 255),
                    "OK" => Color.FromArgb(220, 245, 220),
                    _ => Color.White
                };

                if (back != Color.White)
                {
                    using var b = new SolidBrush(back);
                    g.FillRectangle(b, bounds.Left, y, bounds.Width, rowHeight);
                }

                x = bounds.Left;
                foreach (var col in printCols)
                {
                    var val = GetCellString(row, col.Prop);
                    var rect = new RectangleF(x + 2, y + 2, col.W - 4, rowHeight - 2);
                    g.DrawString(val, font, Brushes.Black, rect, format);
                    x += col.W;
                }

                y += rowHeight;
                _printRowIndex++;
            }

            e.HasMorePages = _printRowIndex < _view.Count;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_currentTheme.HasOuterBorder3D)
            {
                ControlPaint.DrawBorder3D(e.Graphics,
                    new Rectangle(0, 0, Width, Height),
                    Border3DStyle.Raised);
                return;
            }

            switch (_currentTheme.Id)
            {
                case ThemeId.Aero7:
                    using (var outer = new Pen(Color.FromArgb(110, 150, 205), 1f))
                    using (var inner = new Pen(Color.FromArgb(180, 225, 245), 1f))
                    {
                        e.Graphics.DrawRectangle(outer, 0, 0, Width - 1, Height - 1);
                        e.Graphics.DrawRectangle(inner, 1, 1, Width - 3, Height - 3);
                    }
                    break;

                case ThemeId.ModernLight:
                case ThemeId.ModernDark:
                    using (var border = new Pen(_currentTheme.BorderColor, 1f))
                    using (var accent = new Pen(Color.FromArgb(180, _currentTheme.AccentColor), 1f))
                    {
                        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
                        e.Graphics.DrawLine(accent, 0, 0, Width - 1, 0);
                    }
                    break;

                case ThemeId.Fluent11Light:
                case ThemeId.Fluent11Dark:
                    using (var border = new Pen(Color.FromArgb(190, _currentTheme.BorderColor), 1f))
                    using (var glow = new Pen(Color.FromArgb(80, _currentTheme.AccentColor), 1f))
                    {
                        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
                        e.Graphics.DrawRectangle(glow, 1, 1, Width - 3, Height - 3);
                    }
                    break;

                default:
                    using (var pen = new Pen(_currentTheme.BorderColor))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                    break;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
                var screen = Screen.FromHandle(Handle);
                var workArea = screen.WorkingArea;

                mmi.ptMaxPosition.X = workArea.Left;
                mmi.ptMaxPosition.Y = workArea.Top;
                mmi.ptMaxSize.X = workArea.Width;
                mmi.ptMaxSize.Y = workArea.Height;
                mmi.ptMaxTrackSize.X = workArea.Width;
                mmi.ptMaxTrackSize.Y = workArea.Height;

                System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, m.LParam, true);
                return;
            }

            base.WndProc(ref m);
        }
    }
}
