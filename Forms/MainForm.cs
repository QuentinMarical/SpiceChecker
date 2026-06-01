using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpiceChecker.Models;
using SpiceChecker.Rules;
using SpiceChecker.Services;

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
        private ToolStrip _toolbar;
        private ToolStripButton _btnOpen, _btnExport, _btnPrint, _btnCopy, _btnAbout;
        private ToolStripLabel _lblSource;

        // Filtres
        private Panel _filterPanel;
        private TextBox _txtSearch;
        private ComboBox _cbSousEtat, _cbCategorie;
        private CheckBox _chkAnomaliesOnly;
        private Label _lblCount;

        // Grille + statut
        private DataGridView _grid;
        private StatusStrip _status;
        private ToolStripStatusLabel _statusLabel;

        // Pour l'impression
        private int _printRowIndex = 0;

        public MainForm()
        {
            InitializeComponent();
            BuildUi();
            WireEvents();
            UpdateCount();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1280, 760);
            this.Name = "MainForm";
            this.Text = "Spice Checker — Analyse stock SPICE";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AllowDrop = true;
            this.MinimumSize = new Size(960, 540);
            this.ResumeLayout(false);
        }

        private void BuildUi()
        {
            // Toolbar
            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            _btnOpen = new ToolStripButton("Ouvrir XLSX...");
            _btnExport = new ToolStripButton("Export CSV") { Enabled = false };
            _btnPrint = new ToolStripButton("Imprimer") { Enabled = false };
            _btnCopy = new ToolStripButton("Copier sélection") { Enabled = false };
            _btnAbout = new ToolStripButton("À propos");
            _lblSource = new ToolStripLabel("Aucun fichier chargé") { ForeColor = Color.Gray };

            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                _btnOpen, new ToolStripSeparator(),
                _btnExport, _btnPrint, _btnCopy,
                new ToolStripSeparator(), _btnAbout,
                new ToolStripSeparator(), _lblSource
            });

            // Filtres
            _filterPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

            var lblSearch = new Label { Text = "Recherche :", AutoSize = true, Location = new Point(8, 14) };
            _txtSearch = new TextBox
            {
                Location = new Point(80, 10),
                Width = 260,
                PlaceholderText = "Tag, modèle, NS, utilisateur..."
            };

            var lblSE = new Label { Text = "Sous-état :", AutoSize = true, Location = new Point(360, 14) };
            _cbSousEtat = new ComboBox { Location = new Point(430, 10), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblCat = new Label { Text = "Catégorie :", AutoSize = true, Location = new Point(625, 14) };
            _cbCategorie = new ComboBox { Location = new Point(695, 10), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };

            _chkAnomaliesOnly = new CheckBox { Text = "Anomalies uniquement", AutoSize = true, Location = new Point(895, 12) };
            _lblCount = new Label { Text = "0 / 0", AutoSize = true, Location = new Point(1080, 14), Font = new Font(Font, FontStyle.Bold) };

            _filterPanel.Controls.AddRange(new Control[]
            { lblSearch, _txtSearch, lblSE, _cbSousEtat, lblCat, _cbCategorie, _chkAnomaliesOnly, _lblCount });

            // Grille
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None
            };
            ConfigureColumns();
            _bs.DataSource = _view;
            _grid.DataSource = _bs;

            // Status
            _status = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Prêt — glisse un fichier XLSX ou clique Ouvrir");
            _status.Items.Add(_statusLabel);

            Controls.Add(_grid);
            Controls.Add(_filterPanel);
            Controls.Add(_toolbar);
            Controls.Add(_status);
        }

        private void ConfigureColumns()
        {
            _grid.Columns.Clear();
            void Add(string prop, string header, int w)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = prop,
                    HeaderText = header,
                    Width = w,
                    Name = "col_" + prop
                });
            }
            Add("AssetTag", "Étiquette", 110);
            Add("Etat", "État", 90);
            Add("SousEtat", "Sous-état", 170);
            Add("Entrepot", "Entrepôt", 110);
            Add("CategorieModele", "Catégorie", 130);
            Add("Fabricant", "Fabricant", 100);
            Add("Modele", "Modèle", 200);
            Add("RamGo", "RAM (Go)", 70);
            Add("NumeroSerie", "N° série", 130);
            Add("AffecteA", "Affecté à", 150);
            Add("AnomalieNiveau", "Niveau", 90);
            Add("AnomalieMessage", "Anomalie", 280);
            Add("SousEtatConseille", "Conseil", 150);
        }
        // ==================================================================
        //  Événements
        // ==================================================================
        private void WireEvents()
        {
            _btnOpen.Click += (s, e) => OpenFileDialogAndLoad();
            _btnExport.Click += (s, e) => ExportCsv();
            _btnPrint.Click += (s, e) => PrintGrid();
            _btnCopy.Click += (s, e) => CopySelection();
            _btnAbout.Click += (s, e) => MessageBox.Show(
                "Spice Checker\nAnalyse XLSX SPICE / ServiceNow\n\nMARICAL Quentin — 2026",
                "À propos", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            _cbSousEtat.SelectedIndexChanged += (s, e) => ApplyFilters();
            _cbCategorie.SelectedIndexChanged += (s, e) => ApplyFilters();
            _chkAnomaliesOnly.CheckedChanged += (s, e) => ApplyFilters();

            _grid.CellFormatting += Grid_CellFormatting;

            // Drag & drop
            this.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) =>
            {
                if (e.Data == null) return;
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var xlsx = files.FirstOrDefault(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
                if (xlsx != null) LoadFile(xlsx);
            };

            // Raccourcis clavier
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.O) { OpenFileDialogAndLoad(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.F) { _txtSearch.Focus(); _txtSearch.SelectAll(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.E && _btnExport.Enabled) { ExportCsv(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.P && _btnPrint.Enabled) { PrintGrid(); e.Handled = true; }
            };
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
                _btnExport.Enabled = _btnPrint.Enabled = _btnCopy.Enabled = _allRows.Count > 0;

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
        }

        private void ApplyFilters()
        {
            var search = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();
            var seSel = _cbSousEtat.SelectedItem as string;
            var catSel = _cbCategorie.SelectedItem as string;
            bool onlyAno = _chkAnomaliesOnly.Checked;

            IEnumerable<HardwareRow> q = _allRows;

            if (!string.IsNullOrEmpty(seSel) && seSel != "(tous)")
                q = q.Where(r => string.Equals(r.SousEtat, seSel, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(catSel) && catSel != "(toutes)")
                q = q.Where(r => string.Equals(r.CategorieModele, catSel, StringComparison.OrdinalIgnoreCase));

            if (onlyAno)
                q = q.Where(r => !string.IsNullOrEmpty(r.AnomalieNiveau) && r.AnomalieNiveau != "OK");

            if (search.Length > 0)
            {
                q = q.Where(r =>
                    (r.AssetTag ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Modele ?? "").ToLowerInvariant().Contains(search) ||
                    (r.Fabricant ?? "").ToLowerInvariant().Contains(search) ||
                    (r.NumeroSerie ?? "").ToLowerInvariant().Contains(search) ||
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
        }

        // ==================================================================
        //  Coloration des lignes selon le niveau d'anomalie
        // ==================================================================
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _view.Count) return;
            var row = _view[e.RowIndex];
            if (row == null) return;

            Color back;
            switch (row.AnomalieNiveau)
            {
                case "Erreur": back = Color.FromArgb(255, 220, 220); break;
                case "Avertissement": back = Color.FromArgb(255, 240, 200); break;
                case "Info": back = Color.FromArgb(220, 235, 255); break;
                default: back = Color.White; break;
            }
            e.CellStyle.BackColor = back;
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
                            .Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

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
                        .Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

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

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            var bounds = e.MarginBounds;
            using var font = new Font("Segoe UI", 8f);
            using var bold = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var title = new Font("Segoe UI", 12f, FontStyle.Bold);

            // Titre
            string header = "Spice Checker — " + _lblSource.Text +
                            "    (" + _view.Count + " lignes filtrées)    " +
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            g.DrawString(header, title, Brushes.Black, bounds.Left, bounds.Top);
            float y = bounds.Top + 28;

            // Colonnes (sélection pour tenir en paysage)
            var printCols = new (string Prop, string Header, float W)[]
            {
                ("AssetTag",         "Étiquette", 80),
                ("Etat",             "État",      60),
                ("SousEtat",         "Sous-état", 110),
                ("Entrepot",         "Entrepôt",  80),
                ("CategorieModele",  "Catégorie", 90),
                ("Fabricant",        "Fabricant", 70),
                ("Modele",           "Modèle",    140),
                ("RamGo",            "RAM",       40),
                ("NumeroSerie",      "N° série",  100),
                ("AnomalieNiveau",   "Niveau",    60),
                ("AnomalieMessage",  "Anomalie",  180)
            };

            // En-tête colonnes
            float x = bounds.Left;
            using (var headerBrush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(headerBrush, bounds.Left, y, bounds.Width, 20);

            foreach (var col in printCols)
            {
                g.DrawString(col.Header, bold, Brushes.Black, x + 2, y + 3);
                x += col.W;
            }
            y += 22;

            // Lignes
            float rowHeight = font.GetHeight(g) + 4;
            while (_printRowIndex < _view.Count && y + rowHeight < bounds.Bottom)
            {
                var row = _view[_printRowIndex];

                Color back = row.AnomalieNiveau switch
                {
                    "Erreur" => Color.FromArgb(255, 220, 220),
                    "Avertissement" => Color.FromArgb(255, 240, 200),
                    "Info" => Color.FromArgb(220, 235, 255),
                    _ => Color.White
                };
                if (back != Color.White)
                    using (var b = new SolidBrush(back))
                        g.FillRectangle(b, bounds.Left, y, bounds.Width, rowHeight);

                x = bounds.Left;
                foreach (var col in printCols)
                {
                    var val = GetCellString(row, col.Prop);
                    var rect = new RectangleF(x + 2, y + 2, col.W - 4, rowHeight - 2);
                    g.DrawString(val, font, Brushes.Black, rect,
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                    x += col.W;
                }
                y += rowHeight;
                _printRowIndex++;
            }

            e.HasMorePages = _printRowIndex < _view.Count;
        }
    }
}