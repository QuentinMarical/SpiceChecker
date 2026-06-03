using System;
using System.Text.RegularExpressions;
using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle métier : Lenovo L13 G1 / L13 G2 / L14 (8 Go RAM) avec logique date de renouvellement.
    /// Directive Protois Jérémy / Econocom — post 01/04/2026
    /// 
    /// Table de décision :
    /// - L13 G1 / 8 Go / Fonctionnel → Disponible Re-Use
    /// - L13 G1 / 8 Go / Défectueux → Revalorisation (toujours)
    /// - L13 G2 / 8 Go / Fonctionnel → Disponible Re-Use
    /// - L13 G2 / 8 Go / Défectueux / Renouv. ≤ 2027 → Revalorisation
    /// - L13 G2 / 8 Go / Défectueux / Renouv. ≥ 2028 → Réparation
    /// - L14 / 8 Go / Fonctionnel → Disponible Re-Use
    /// - L14 / 8 Go / Défectueux / Renouv. ≤ 2027 → Revalorisation
    /// - L14 / 8 Go / Défectueux / Renouv. ≥ 2028 → Réparation
    /// </summary>
    public class L13L14RenewalRule : IRule
    {
        public string Nom => "L13L14-Renouvellement";

        // Expressions régulières pour détecter les modèles
        private static readonly Regex L13G1Pattern = new Regex(
            @"thinkpad\s*(l13|l-13)\s*(g1|gen1|1(?:e|er|ère)\s*g(?:é|e)n(?:é|e)ration?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex L13G2Pattern = new Regex(
            @"thinkpad\s*(l13|l-13)\s*(g2|gen2|2(?:e|d)(?:e|è)me?\s*g(?:é|e)n(?:é|e)ration?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex L14Pattern = new Regex(
            @"thinkpad\s*(l14|l-14)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Codes produits ThinkPad alternatifs
        private static readonly Regex L13G1AltPattern = new Regex(
            @"20u[1-9]\d|20ug",  // Codes produits ThinkPad L13 G1
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex L13G2AltPattern = new Regex(
            @"21b[1-9]\d|21ba|21bb",  // Codes produits ThinkPad L13 G2
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex L14G1AltPattern = new Regex(
            @"20u[1-9]\d|20u5|20u6|20u9",  // Codes produits ThinkPad L14 G1
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex L14G2AltPattern = new Regex(
            @"20x[1-9]\d|20x2|20x3",  // Codes produits ThinkPad L14 G2
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            // Seulement les Lenovo avec 8 Go RAM
            bool isLenovo = !string.IsNullOrEmpty(row.Fabricant) &&
                            row.Fabricant.IndexOf("lenovo", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isLenovo) return EvaluationResult.Ok();

            bool is8GbRam = row.RamGo.HasValue && row.RamGo.Value == 8;
            if (!is8GbRam) return EvaluationResult.Ok();

            // Le matériel doit être un L13 G1, L13 G2, ou L14
            if (!IsL13G1(row) && !IsL13G2(row) && !IsL14(row))
                return EvaluationResult.Ok();

            // Évaluer le matériel selon son état
            bool isL13G1 = IsL13G1(row);
            bool isL13G2 = IsL13G2(row);
            bool isL14 = IsL14(row);

            bool isFunctional = IsFunctional(row);
            bool isDefective = IsDefective(row);

            // Si ni fonctionnel ni défectueux (état inconnu), pas d'anomalie
            if (!isFunctional && !isDefective)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Info,
                    Message = "État du matériel non déterminé — vérification manuelle requise",
                    SousEtatConseille = ""
                };
            }

            // ──────────────────────────────────────────────────────────────
            //  CAS 1 : FONCTIONNEL
            // ──────────────────────────────────────────────────────────────
            if (isFunctional)
            {
                string currentSubstate = row.SousEtat ?? string.Empty;
                string modelLabel = GetModelLabel(row);

                // OK si déjà en Disponible Re-Use ou sous-états acceptables
                if (SubstateMatches(currentSubstate, "Disponible Re-Use") ||
                    SubstateMatches(currentSubstate, "Disponible neuf") ||
                    SubstateMatches(currentSubstate, "Reprise en attente") ||
                    SubstateMatches(currentSubstate, "Réservé/Masterisé"))
                {
                    return EvaluationResult.Ok();
                }

                // Anomalie : fonctionnel mais dans un autre sous-état
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Avertissement,
                    Message = $"ANOMALIE : {modelLabel} fonctionnel devrait être en Disponible Re-Use, pas en '{currentSubstate}'",
                    SousEtatConseille = "Disponible Re-Use"
                };
            }

            // ──────────────────────────────────────────────────────────────
            //  CAS 2 : DÉFECTUEUX — logique dépend du modèle et de la date
            // ──────────────────────────────────────────────────────────────
            int? renewalYear = ExtractRenewalYear(row);
            string currentSubstate_def = row.SousEtat ?? string.Empty;
            string modelLabel_def = GetModelLabel(row);

            if (isL13G1)
            {
                // L13 G1 déf. → toujours Revalorisation
                string actionTarget = "Revalorisation";

                if (SubstateMatches(currentSubstate_def, actionTarget))
                {
                    // OK - commentaire de panne obligatoire ?
                    if (!HasDefectComment(row))
                    {
                        return new EvaluationResult
                        {
                            EstAnomalie = true,
                            Niveau = NiveauAnomalie.Erreur,
                            Message = $"{modelLabel_def} défectueux en {actionTarget} — ⚠ COMMENTAIRE DE PANNE OBLIGATOIRE",
                            SousEtatConseille = actionTarget
                        };
                    }
                    return EvaluationResult.Ok();
                }

                // En transition ou anomalie
                if (SubstateMatches(currentSubstate_def, "Reprise en attente") ||
                    SubstateMatches(currentSubstate_def, "Réservé/Masterisé"))
                {
                    return EvaluationResult.Ok(); // Transition acceptable
                }

                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Avertissement,
                    Message = $"ANOMALIE : {modelLabel_def} défectueux devrait être en {actionTarget}, pas en '{currentSubstate_def}'",
                    SousEtatConseille = actionTarget
                };
            }

            if (isL13G2 || isL14)
            {
                // Date manquante → avertissement
                if (renewalYear == null)
                {
                    return new EvaluationResult
                    {
                        EstAnomalie = true,
                        Niveau = NiveauAnomalie.Avertissement,
                        Message = $"⚠ Date de renouvellement manquante pour {modelLabel_def} défectueux — vérification manuelle requise",
                        SousEtatConseille = ""
                    };
                }

                string actionTarget = renewalYear.Value <= 2027 ? "Revalorisation" : "Réparation";

                if (SubstateMatches(currentSubstate_def, actionTarget))
                {
                    // OK - commentaire de panne obligatoire ?
                    if (!HasDefectComment(row))
                    {
                        return new EvaluationResult
                        {
                            EstAnomalie = true,
                            Niveau = NiveauAnomalie.Erreur,
                            Message = $"{modelLabel_def} défectueux en {actionTarget} (renouv. {renewalYear}) — ⚠ COMMENTAIRE DE PANNE OBLIGATOIRE",
                            SousEtatConseille = actionTarget
                        };
                    }
                    return EvaluationResult.Ok();
                }

                // En transition ou anomalie
                if (SubstateMatches(currentSubstate_def, "Reprise en attente") ||
                    SubstateMatches(currentSubstate_def, "Réservé/Masterisé"))
                {
                    return EvaluationResult.Ok(); // Transition acceptable
                }

                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Avertissement,
                    Message = $"ANOMALIE : {modelLabel_def} défectueux (renouv. {renewalYear}) devrait être en {actionTarget}, pas en '{currentSubstate_def}'",
                    SousEtatConseille = actionTarget
                };
            }

            return EvaluationResult.Ok();
        }

        // ──────────────────────────────────────────────────────────────
        //  Méthodes utilitaires
        // ──────────────────────────────────────────────────────────────
        private bool IsL13G1(HardwareRow row)
        {
            if (string.IsNullOrEmpty(row.Modele)) return false;
            return L13G1Pattern.IsMatch(row.Modele) || L13G1AltPattern.IsMatch(row.Modele);
        }

        private bool IsL13G2(HardwareRow row)
        {
            if (string.IsNullOrEmpty(row.Modele)) return false;
            return L13G2Pattern.IsMatch(row.Modele) || L13G2AltPattern.IsMatch(row.Modele);
        }

        private bool IsL14(HardwareRow row)
        {
            if (string.IsNullOrEmpty(row.Modele)) return false;
            return L14Pattern.IsMatch(row.Modele) || L14G1AltPattern.IsMatch(row.Modele) || L14G2AltPattern.IsMatch(row.Modele);
        }

        private string GetModelLabel(HardwareRow row)
        {
            if (IsL13G1(row)) return "ThinkPad L13 G1 (8 Go)";
            if (IsL13G2(row)) return "ThinkPad L13 G2 (8 Go)";
            if (IsL14(row)) return "ThinkPad L14 (8 Go)";
            return "Lenovo 8 Go";
        }

        private bool IsFunctional(HardwareRow row)
        {
            var etat = (row.Etat ?? "").ToLowerInvariant();
            var sousEtat = (row.SousEtat ?? "").ToLowerInvariant();

            // Fonctionnel si état est "En service", "Opérationnel", "Fonctionnel"
            // OU sous-état est "Disponible neuf", "Disponible Re-Use", etc.
            return etat.Contains("en service") ||
                   etat.Contains("operationnel") ||
                   etat.Contains("fonctionnel") ||
                   etat.Contains("ok") ||
                   etat.Contains("disponible") ||
                   sousEtat.Contains("disponible") ||
                   sousEtat.Contains("re-use") ||
                   sousEtat.Contains("reuse");
        }

        private bool IsDefective(HardwareRow row)
        {
            var etat = (row.Etat ?? "").ToLowerInvariant();
            var sousEtat = (row.SousEtat ?? "").ToLowerInvariant();
            var description = (row.ToString() ?? "").ToLowerInvariant();

            return etat.Contains("hs") ||
                   etat.Contains("defect") ||
                   etat.Contains("panne") ||
                   etat.Contains("h.s.") ||
                   sousEtat.Contains("defect") ||
                   sousEtat.Contains("hs") ||
                   sousEtat.Contains("revalorisation");
        }

        private int? ExtractRenewalYear(HardwareRow row)
        {
            try
            {
                if (row.DateRenouvellement.HasValue)
                    return row.DateRenouvellement.Value.Year;

                // Fallback : tentative depuis DateSousEtat si present
                if (row.DateSousEtat.HasValue)
                    return row.DateSousEtat.Value.Year;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool HasDefectComment(HardwareRow row)
        {
            // Vérifier la présence d'une description de panne (au moins 10 caractères)
            var desc = (row.ToString() ?? "").Trim();
            return !string.IsNullOrEmpty(desc) && desc.Length >= 10;
        }

        private bool SubstateMatches(string actual, string target)
        {
            if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(target))
                return false;
            return actual.Equals(target, StringComparison.OrdinalIgnoreCase);
        }
    }
}

