using System.Globalization;
using System.Text;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Interdit la revalorisation d'un ordinateur sans justification explicite de défaut.
/// </summary>
public sealed class RevalorisationSansDefautRule : IRule
{
    private static readonly string[] DefectKeywords =
    {
        "defect",
        "defectueux",
        "panne",
        "hs",
        "casse"
    };

    public string Name => "RevalorisationSansDefautRule";

    public bool IsOverride => false;

    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var isComputerInRevalorisation =
            asset.Categorie == CategorieEquipement.Ordinateur &&
            asset.SousEtat == SousEtat.Revalorisation;

        if (!isComputerInRevalorisation)
        {
            return null;
        }

        var normalizedComment = Normalize(asset.Commentaire);
        var hasDefectJustification =
            !string.IsNullOrWhiteSpace(normalizedComment) &&
            ContainsAnyKeyword(normalizedComment, DefectKeywords);

        if (!hasDefectJustification)
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = Name,
                Message = "Un ordinateur ne peut être en Revalorisation sans justification explicite de défaut.",
                EstBloquant = true
            };
        }

        return null;
    }

    private static bool ContainsAnyKeyword(string content, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (content.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);

        foreach (var c in formD)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}