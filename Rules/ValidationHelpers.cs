using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Helpers de validation métier communs aux différentes règles.
    /// </summary>
    public static class ValidationHelpers
    {
        /// <summary>
        /// Vérifie si un commentaire de panne est présent et suffisamment détaillé.
        /// Retourne (isValid, messageIfMissing).
        /// </summary>
        public static (bool IsValid, string MissingMessage) ValidateDefectComment(HardwareRow row, string actionTarget)
        {
            var description = (row.Description ?? "").Trim();

            // Réservé/Masterisé ou Reprise en attente : pas de commentaire requis
            var sousEtat = (row.SousEtat ?? "").ToLowerInvariant();
            if (sousEtat.Contains("réservé") || sousEtat.Contains("masterisé") ||
                sousEtat.Contains("reprise en attente"))
            {
                return (true, string.Empty);
            }

            // Disponible neuf : pas de commentaire requis (jamais une anomalie de ce type)
            if (sousEtat.Contains("disponible neuf"))
            {
                return (true, string.Empty);
            }

            // Vérifier le contenu du commentaire
            if (string.IsNullOrEmpty(description))
            {
                return (false, $"⚠ COMMENTAIRE DE PANNE OBLIGATOIRE pour {actionTarget} — Description vide");
            }

            // Minimum 10 caractères pour être considéré comme un vrai commentaire
            if (description.Length < 10)
            {
                return (false, $"⚠ COMMENTAIRE DE PANNE TROP COURT pour {actionTarget} — Décris la nature de la panne (minimum 10 caractères)");
            }

            // Keywords suspects : commentaire qui ne décrit pas une panne
            var nonDefectKeywords = new[] { "ok", "fonctionnel", "rien", "ras", "ok ok", "test ok" };
            var descLower = description.ToLowerInvariant();
            foreach (var kw in nonDefectKeywords)
            {
                if (descLower.Contains(kw))
                {
                    // C'est acceptable si le commentaire mentionne quand même une panne
                    if (!descLower.Contains("panne") && !descLower.Contains("defect")
                        && !descLower.Contains("h.s") && !descLower.Contains("hs ")
                        && !descLower.Contains("ne démarre") && !descLower.Contains("ne s'allume")
                        && !descLower.Contains("écran") && !descLower.Contains("clavier")
                        && !descLower.Contains("batterie") && !descLower.Contains("charge")
                        && !descLower.Contains("broken") && !descLower.Contains("fail"))
                    {
                        return (false, $"⚠ COMMENTAIRE SUSPECT : '{description}' ne décrit pas clairement une panne pour {actionTarget}");
                    }
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Applique la validation du commentaire sur un EvaluationResult existant.
        /// Si le commentaire est manquant et que l'action recommandée est Revalorisation ou Réparation,
        /// élève le niveau de l'anomalie à Erreur.
        /// </summary>
        public static void ApplyDefectCommentCheck(EvaluationResult result, HardwareRow row)
        {
            if (result == null || row == null) return;

            if (result.Niveau == NiveauAnomalie.Erreur) return;

            var action = (result.SousEtatConseille ?? "").ToLowerInvariant();
            if (action.Contains("revalorisation") || action.Contains("réparation") || action.Contains("reparation"))
            {
                var (isValid, message) = ValidateDefectComment(row, result.SousEtatConseille ?? "cette action");
                if (!isValid)
                {
                    result.Message = string.IsNullOrWhiteSpace(result.Message)
                        ? message
                        : result.Message + " " + message;
                    result.Niveau = NiveauAnomalie.Erreur;
                    result.EstAnomalie = true;
                }
            }
        }
    }
}
