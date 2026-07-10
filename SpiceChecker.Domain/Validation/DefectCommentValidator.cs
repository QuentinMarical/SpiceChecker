using System.Globalization;
using System.Text;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Validation;

/// <summary>
/// Valide les commentaires liés aux pannes matériel.
/// </summary>
public sealed class DefectCommentValidator : IDefectCommentValidator
{
    private static readonly string[] RepairTerms =
    {
        "a reparer",
        "reparation",
        "renvoyer en repar",
        "retour sav"
    };

    private const int LongueurMinimaleCommentairePanne = 10;

    /// <inheritdoc />
    public ValidationResult Validate(string commentaire, SousEtat sousEtat)
    {
        var value = commentaire ?? string.Empty;

        if (sousEtat == SousEtat.Defectueux && string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Failure("Commentaire de panne obligatoire pour le sous-état Défectueux (nature de la panne).");
        }

        if (sousEtat == SousEtat.Defectueux && value.Trim().Length < LongueurMinimaleCommentairePanne)
        {
            return ValidationResult.Failure($"Commentaire de panne trop court (minimum {LongueurMinimaleCommentairePanne} caractères) : décrire la nature de la panne.");
        }

        // Un matériel destiné à la réparation ne doit pas être en Revalorisation.
        if (sousEtat == SousEtat.Revalorisation && ContainsRepairTerm(Normalize(value)))
        {
            return ValidationResult.Failure("Le commentaire indique une réparation : ce matériel doit partir en Réparation, pas en Revalorisation.");
        }

        return ValidationResult.Success();
    }

    private static bool ContainsRepairTerm(string normalizedComment)
    {
        foreach (var term in RepairTerms)
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
