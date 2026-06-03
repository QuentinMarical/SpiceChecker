using SpiceChecker.Models;
using SpiceChecker.Rules;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle ² — Lorskl'estat global est "En stock" mais le sous-état est incohérent
    /// avec le mode sonore ou les commentaires : "Défectueux", "HS", "Panne", etc.
    /// Cela signale un matériel à ne PAS mettre en Re-Use tant que le statut défectueux
    /// n'est pas résolu.
    /// </summary>
    public class DefectiveStateRule : IRule
    {
        public string Nom => "État défectueux ignoré";

        private static readonly string[] _termsDefectueux = new[]
        {
            "dectueux", "HS", "panne", "cass", "cassé", "HS",
            "ne foncti", "ne fonctionne", "dead", "broken", "fault"
        };

        public EvaluationResult Evaluate(HardwareRow row)
        {
            var result = EvaluationResult.Ok();
            if (row == null) return result;

            var sousEtat  = (row.SousEtat  ?? "").ToLowerInvariant();
            var commentaires = (row.ToString().ToLowerInvariant()); // placeholder, on n'a pas la colonne ici

            // Si le sous-état est "Défectueux" officiel mais que le matériel est listé
            // avec un état potentiellement disponible (Re-Use, Disponible neuf) → contradiction
            bool sousEtatDefectueux = sousEtat.Contains("dectueux") || sousEtat.Contains("hs");
            bool sousEtatDisponible = sousEtat.Contains("re-use") || sousEtat.Contains("reuse")
                                   || sousEtat.Contains("disponible") || sousEtat.Contains("dispo neuf")
                                   || sousEtat.Contains("reprise en attente");

            if (sousEtatDefectueux && sousEtatDisponible)
            {
                result = new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Erreur,
                    Message = $"Sous-état contradictoire : défectueux ET disponible ({row.SousEtat})",
                    SousEtatConseille = "Défectueux"
                };
            }

            ValidationHelpers.ApplyDefectCommentCheck(result, row);
            return result;
        }
    }
}
