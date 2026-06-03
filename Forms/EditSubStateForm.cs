using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SpiceChecker
{
    /// <summary>
    /// Formulaire de dialogue pour modifier le sous-état d'une ligne du DataGridView.
    /// </summary>
    public class EditSubStateForm : Form
    {
        private ComboBox _cbSubState = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;

        public string SelectedSubState { get; private set; } = "";

        private static readonly string[] ValidSubStates = new[]
        {
            "Disponible neuf",
            "Disponible Re-Use",
            "A blanchir",
            "Défectueux",
            "Revalorisation Dclass",
            "Retour loueur",
            "En attente de don",
            "Reprise en attente",
            "Réservé/Masterisé"
        };

        public EditSubStateForm(string currentSubState)
        {
            InitializeComponent();
            SelectedSubState = currentSubState;
            _cbSubState.SelectedItem = currentSubState;
            if (_cbSubState.SelectedIndex == -1 && !string.IsNullOrEmpty(currentSubState))
                _cbSubState.Text = currentSubState;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(400, 140);
            this.Name = "EditSubStateForm";
            this.Text = "Modifier le sous-état";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Label
            var lblSubState = new Label
            {
                Text = "Nouveau sous-état :",
                AutoSize = true,
                Location = new Point(12, 15)
            };

            // ComboBox
            _cbSubState = new ComboBox
            {
                Location = new Point(12, 35),
                Width = 360,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            foreach (var subState in ValidSubStates)
                _cbSubState.Items.Add(subState);

            // Bouton OK
            _btnOk = new Button
            {
                Text = "OK",
                Location = new Point(232, 75),
                Width = 70,
                Height = 25,
                DialogResult = DialogResult.OK
            };
            _btnOk.Click += (s, e) =>
            {
                SelectedSubState = _cbSubState.Text.Trim();
                if (string.IsNullOrEmpty(SelectedSubState))
                {
                    MessageBox.Show(this, "Veuillez saisir ou sélectionner un sous-état.",
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                this.Close();
            };

            // Bouton Annuler
            _btnCancel = new Button
            {
                Text = "Annuler",
                Location = new Point(310, 75),
                Width = 70,
                Height = 25,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { lblSubState, _cbSubState, _btnOk, _btnCancel });
            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
