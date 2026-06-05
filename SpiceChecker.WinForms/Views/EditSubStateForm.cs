using SpiceChecker.Domain.Enums;

namespace SpiceChecker.WinForms.Views;

/// <summary>
/// Formulaire minimal d'édition du sous-état et du commentaire.
/// </summary>
public sealed class EditSubStateForm : Form
{
    private readonly ComboBox _cmbSousEtat;
    private readonly TextBox _txtCommentaire;

    public EditSubStateForm(SousEtat currentSousEtat, string currentCommentaire)
    {
        Text = "Modifier le sous-état";
        Width = 460;
        Height = 280;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;

        var lblSousEtat = new Label { Left = 12, Top = 16, Width = 120, Text = "Sous-état" };
        _cmbSousEtat = new ComboBox
        {
            Left = 12,
            Top = 36,
            Width = 420,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _cmbSousEtat.Items.AddRange(Enum.GetValues<SousEtat>().Cast<object>().ToArray());
        _cmbSousEtat.SelectedItem = currentSousEtat;

        var lblCommentaire = new Label { Left = 12, Top = 72, Width = 120, Text = "Commentaire" };
        _txtCommentaire = new TextBox
        {
            Left = 12,
            Top = 92,
            Width = 420,
            Height = 110,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = currentCommentaire ?? string.Empty
        };

        var btnOk = new Button { Left = 276, Top = 210, Width = 75, Text = "OK", DialogResult = DialogResult.OK };
        var btnCancel = new Button { Left = 357, Top = 210, Width = 75, Text = "Annuler", DialogResult = DialogResult.Cancel };

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Controls.Add(lblSousEtat);
        Controls.Add(_cmbSousEtat);
        Controls.Add(lblCommentaire);
        Controls.Add(_txtCommentaire);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
    }

    public SousEtat ResultatSousEtat => _cmbSousEtat.SelectedItem is SousEtat value ? value : SousEtat.Autre;

    public string ResultatCommentaire => _txtCommentaire.Text ?? string.Empty;
}
