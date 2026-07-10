using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle 3 — Tout ordinateur en Revalorisation sans être défectueux est une anomalie.
    /// </summary>
    public class RevalorisationSansDefautRule : IRule
    {
        public string Nom => "Revalorisation sans défaut";
        public bool IsOverride => false;

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            bool estOrdinateur = row.CategorieModele?.Contains("ordinateur", StringComparison.OrdinalIgnoreCase) == true;
            if (!estOrdinateur) return EvaluationResult.Ok();

            bool enRevalorisation = row.SousEtat?.Contains("revalorisation", StringComparison.OrdinalIgnoreCase) == true;
            bool estDefectueux = row.SousEtat?.Contains("défectueux", StringComparison.OrdinalIgnoreCase) == true;

            if (enRevalorisation && !estDefectueux)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Erreur,
                    Message = "Matériel en Revalorisation sans être défectueux",
                    SousEtatConseille = "Requalifier en Disponible Re-Use si fonctionnel"
                };
            }

            return EvaluationResult.Ok();
        }
    }
}
