using System.Globalization;
using System.Text;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Validation;

/// <summary>
/// Valide les commentaires liés aux pannes matériel.
/// </summary>
public sealed class DefectCommentValidator : IDefectCommentValidator
{
    private static readonly string[] ForbiddenTerms =
    {
        "a reparer",
        "en panne",
        "reparation"
    };

    /// <inheritdoc />
    public ValidationResult Validate(string commentaire, SousEtat sousEtat)
    {
        var value = commentaire ?? string.Empty;

        if (sousEtat == SousEtat.Defectueux && string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Failure("Un commentaire est obligatoire pour le sous-état Défectueux.");
        }

        var normalized = Normalize(value);
        if (ContainsForbiddenTerm(normalized))
        {
            return ValidationResult.Failure("Le commentaire ne doit pas contenir de termes liés à la réparation. Utilisez le sous-état \"Défectueux\".");
        }

        return ValidationResult.Success();
    }

    private static bool ContainsForbiddenTerm(string normalizedComment)
    {
        foreach (var term in ForbiddenTerms)
        {
            if (normalizedComment.Contains(term, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string input)
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
