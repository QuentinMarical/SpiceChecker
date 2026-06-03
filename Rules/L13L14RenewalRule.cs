using SpiceChecker.Models;
using SpiceChecker.Services;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle 2 — Lenovo L13 G1 / L13 G2 / L14 (8 Go RAM) avec logique date de renouvellement.
    /// </summary>
    public class L13L14RenewalRule : IRule
    {
        public string Nom => "L13L14-Renouvellement";
        public bool IsOverride => false;

        private static readonly string[] _suffixesG1 = { "21RB", "21RE", "21RC", "21RD" };

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            bool estLenovo = row.Fabricant?.Contains("lenovo", StringComparison.OrdinalIgnoreCase) == true;
            if (!estLenovo) return EvaluationResult.Ok();

            bool is8Go = row.RamGo.HasValue && row.RamGo.Value == 8;
            if (!is8Go) return EvaluationResult.Ok();

            bool estOrdinateur = row.CategorieModele?.Contains("ordinateur", StringComparison.OrdinalIgnoreCase) == true;
            if (!estOrdinateur) return EvaluationResult.Ok();

            bool hasL13 = row.Modele?.Contains("L13", StringComparison.OrdinalIgnoreCase) == true;
            bool hasL14 = row.Modele?.Contains("L14", StringComparison.OrdinalIgnoreCase) == true;

            if (!hasL13 && !hasL14) return EvaluationResult.Ok();

            bool estDefectueux = row.SousEtat?.Contains("défectueux", StringComparison.OrdinalIgnoreCase) == true;

            if (hasL13)
            {
                bool estG1 = IsL13G1(row.Modele);
                if (estG1)
                    return EvaluerL13G1(row, estDefectueux);
                else
                    return EvaluerAvecDateRenouvellement(row, estDefectueux, "L13 G2 8 Go");
            }
            else // L14
            {
                return EvaluerAvecDateRenouvellement(row, estDefectueux, "L14 8 Go");
            }
        }

        private bool IsL13G1(string? modele)
        {
            if (modele == null) return false;
            foreach (var suffix in _suffixesG1)
                if (modele.Contains(suffix, StringComparison.OrdinalIgnoreCase)) return true;
            return modele.Contains("G1", StringComparison.OrdinalIgnoreCase)
                || modele.Contains("Gen 1", StringComparison.OrdinalIgnoreCase);
        }

        private EvaluationResult EvaluerL13G1(HardwareRow row, bool estDefectueux)
        {
            if (!estDefectueux)
            {
                return new EvaluationResult
                {
                    EstAnomalie = false,
                    Message = "L13 G1 8 Go fonctionnel → Disponible Re-Use (incident/contrat court)"
                };
            }

            // Défectueux → Revalorisation obligatoire
            bool enRevalorisation = row.SousEtat?.Contains("revalorisation", StringComparison.OrdinalIgnoreCase) == true;
            if (!enRevalorisation)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Erreur,
                    Message = "L13 G1 8 Go défectueux → doit aller en Revalorisation",
                    SousEtatConseille = "Classer en Revalorisation + ajouter commentaire panne"
                };
            }

            if (!SpiceChecker.Services.ValidationHelpers.HasValidComment(row.Description))
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Erreur,
                    Message = "L13 G1 8 Go en Revalorisation sans commentaire de panne",
                    SousEtatConseille = "Ajouter un commentaire de panne (min. 10 caractères)"
                };
            }

            return EvaluationResult.Ok();
        }

        private EvaluationResult EvaluerAvecDateRenouvellement(HardwareRow row, bool estDefectueux, string label)
        {
            if (!estDefectueux)
            {
                return new EvaluationResult
                {
                    EstAnomalie = false,
                    Message = $"{label} fonctionnel → Disponible Re-Use"
                };
            }

            // Défectueux
            if (!row.DateRenouvellement.HasValue)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Avertissement,
                    Message = $"{label} défectueux — date de renouvellement manquante",
                    SousEtatConseille = "Renseigner la date de renouvellement"
                };
            }

            int annee = row.DateRenouvellement.Value.Year;

            if (annee <= 2027)
            {
                // Revalorisation attendue
                bool enRevalorisation = row.SousEtat?.Contains("revalorisation", StringComparison.OrdinalIgnoreCase) == true;
                if (!enRevalorisation)
                {
                    return new EvaluationResult
                    {
                        EstAnomalie = true,
                        Niveau = NiveauAnomalie.Erreur,
                        Message = $"{label} défectueux → doit aller en Revalorisation",
                        SousEtatConseille = "Classer en Revalorisation + commentaire panne"
                    };
                }

                if (!SpiceChecker.Services.ValidationHelpers.HasValidComment(row.Description))
                {
                    return new EvaluationResult
                    {
                        EstAnomalie = true,
                        Niveau = NiveauAnomalie.Erreur,
                        Message = $"{label} en Revalorisation sans commentaire de panne",
                        SousEtatConseille = "Ajouter un commentaire de panne (min. 10 caractères)"
                    };
                }

                return EvaluationResult.Ok();
            }
            else // annee >= 2028
            {
                // Réparation attendue
                bool enRevalorisation = row.SousEtat?.Contains("revalorisation", StringComparison.OrdinalIgnoreCase) == true;
                if (enRevalorisation)
                {
                    return new EvaluationResult
                    {
                        EstAnomalie = true,
                        Niveau = NiveauAnomalie.Erreur,
                        Message = $"{label} renouvellement ≥ 2028 → Réparation, pas Revalorisation",
                        SousEtatConseille = "Classer en Réparation"
                    };
                }

                bool enReparation = row.SousEtat?.Contains("réparation", StringComparison.OrdinalIgnoreCase) == true
                                 || row.SousEtat?.Contains("reparation", StringComparison.OrdinalIgnoreCase) == true;
                if (!enReparation)
                {
                    return new EvaluationResult
                    {
                        EstAnomalie = true,
                        Niveau = NiveauAnomalie.Erreur,
                        Message = $"{label} défectueux renouvellement ≥ 2028 → doit aller en Réparation",
                        SousEtatConseille = "Classer en Réparation"
                    };
                }

                return EvaluationResult.Ok();
            }
        }
    }
}