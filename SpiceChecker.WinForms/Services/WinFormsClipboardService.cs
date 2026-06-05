using SpiceChecker.Application.Services;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Implémentation WinForms du presse-papier système.
/// </summary>
public sealed class WinFormsClipboardService : IClipboardService
{
    /// <inheritdoc />
    public void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Clipboard.SetText(text);
    }
}
