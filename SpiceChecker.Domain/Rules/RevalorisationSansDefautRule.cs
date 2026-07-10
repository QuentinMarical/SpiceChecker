using System.Globalization;
using System.Text;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Domain.Rules;

/// <summary>
/// Tout ordinateur en Revalorisation doit être défectueux et porter un commentaire
/// indiquant la nature de la panne, sinon il doit être requalifié en Disponible Re-Use.
/// </summary>
public sealed class RevalorisationSansDefautRule : IRule
{
    private static readonly string[] DefectKeywords =
    {
        "defect",
        "defectueux",
        "panne",
        "hs",
        "casse",
        "ecran",
        "dalle",
        "clavier",
        "batterie",
        "charniere",
        "chute",
        "oxyd",
        "demarre",
        "allume",
        "carte mere",
        "disque",
        "ssd",
        "ventil",
        "connecteur",
        "port",
        "tactile",
        "bios",
        "alim"
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

        if (string.IsNullOrWhiteSpace(normalizedComment))
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Avertissement,
                RegleDeclenchee = Name,
                Message = "Revalorisation sans commentaire de panne dans l'export : vérifier dans ServiceNow que la nature de la panne est renseignée (obligatoire), sinon requalifier en Disponible Re-Use.",
                EstBloquant = false
            };
        }

        if (!ContainsAnyKeyword(normalizedComment, DefectKeywords))
        {
            return new EvaluationResult
            {
                Niveau = NiveauAnomalie.Erreur,
                RegleDeclenchee = Name,
                Message = "Revalorisation sans justification de défaut dans le commentaire : compléter la nature de la panne ou requalifier en Disponible Re-Use.",
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
