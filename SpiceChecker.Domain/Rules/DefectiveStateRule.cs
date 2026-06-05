using System.Globalization;
using System.Text;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;
using SpiceChecker.Domain.Validation;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Vérifie la cohérence entre le sous-état de disponibilité et le commentaire de défaut.
/// </summary>
public sealed class DefectiveStateRule : IRule
{
    private static readonly string[] DefectKeywords =
    {
        "defectueux",
        "panne",
        "hs",
        "casse"
    };

    private readonly IDefectCommentValidator _defectCommentValidator;

    /// <summary>
    /// Initialise une nouvelle instance de la règle.
    /// </summary>
    /// <param name="defectCommentValidator">Validateur centralisé de commentaire de panne.</param>
    public DefectiveStateRule(IDefectCommentValidator defectCommentValidator)
    {
        _defectCommentValidator = defectCommentValidator ?? throw new ArgumentNullException(nameof(defectCommentValidator));
    }

    /// <inheritdoc />
    public string Name => "DefectiveStateRule";

    /// <inheritdoc />
    public EvaluationResult? Evaluate(HardwareAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (asset.SousEtat == SousEtat.Defectueux)
        {
            var validation = _defectCommentValidator.Validate(asset.Commentaire, asset.SousEtat);
            if (!validation.IsValid)
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Erreur,
                    RegleDeclenchee = Name,
                    Message = validation.ErrorMessage,
                    EstBloquant = true
                };
            }

            return null;
        }

        if (asset.SousEtat is SousEtat.Disponible or SousEtat.DisponibleNeuf)
        {
            var normalizedComment = Normalize(asset.Commentaire);
            if (ContainsAnyKeyword(normalizedComment, DefectKeywords))
            {
                return new EvaluationResult
                {
                    Niveau = NiveauAnomalie.Avertissement,
                    RegleDeclenchee = Name,
                    Message = "Incohérence : l'équipement est marqué disponible mais le commentaire mentionne un défaut.",
                    EstBloquant = false
                };
            }
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
