using SpiceChecker.Models;
using SpiceChecker.Rules;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle ³ — Identifie les équipements à basculer en "Revalorisation Dclass"
    /// ou "Retour loueur". Critères :
    /// - Sous-état actuel = "Retour loueur", "Revalorisation Dclass"
    /// - OU : modèle vieux (générique), entree stock depuis longtemps,
    ///         fabricant connu pour programme reprise/échange
    /// - OU : équipement réseau Cisco/HP en "Disponible Re-Use" depuis > 90 jours
    /// </summary>
    public class RevalorisationRule : IRule
    {
        public string Nom => "Revalorisation à envisager";

        private static readonly string[] _sousEtatsRevalorisation = new[]
        {
            "Revalorisation Dclass", "Retour loueur", "Revalorisation",
            "Retour fournisseur", "Dclass", "D-Class"
        };

        public EvaluationResult Evaluate(HardwareRow row)
        {
            var result = EvaluationResult.Ok();
            if (row == null) return result;

            var sousEtat = (row.SousEtat ?? "").ToLowerInvariant();

            // Cas 1 : déjà en Revalorisation ou Retour loueur officiels → OK "conforme"
            bool dejaRevalorisation = false;
            foreach (var se in _sousEtatsRevalorisation)
            {
                if (sousEtat.Contains(se.ToLowerInvariant()))
                {
                    dejaRevalorisation = true;
                    break;
                }
            }

            if (!dejaRevalorisation)
            {
                // Cas ² : équipement réseau (Cisco, HP Procurve) en Re-Use sans sous-état structuré
                var modele = (row.Modele ?? "").ToLowerInvariant();
                var fabricant = (row.Fabricant ?? "").ToLowerInvariant();
                var categorie = (row.CategorieModele ?? "").ToLowerInvariant();

                bool estReseau = categorie.Contains("réseau") || categorie.Contains("network")
                              || modele.Contains("cisco") || modele.Contains("catalyst")
                              || modele.Contains("switch ") || modele.Contains("-ws-");

                bool enReUse = sousEtat.Contains("re-use") || sousEtat.Contains("reuse")
                            || sousEtat.Contains("disponible") || sousEtat.Contains("dispo");

                if (estReseau && enReUse && string.IsNullOrEmpty(row.SousEtatConseille))
                {
                    result = new EvaluationResult
                    {
                        EstAnomalie = false,
                        Niveau = NiveauAnomalie.Info,
                        Message = $"Équipement réseau ({row.Fabricant}) en {row.SousEtat} — vérifier si revalorisation applicable",
                        SousEtatConseille = "Revalorisation Dclass / Retour loueur"
                    };
                }
                else
                {
                    // Cas ³ : vieux Lenovo T580/T590 en Re-Use (modèle fin de cycle)
                    bool vieuxModele = modele.Contains("t580") || modele.Contains("t590") ||
                                      modele.Contains("thinkpad_t58") || modele.Contains("thinkpad_t59");

                    if (vieuxModele && enReUse)
                    {
                        result = new EvaluationResult
                        {
                            EstAnomalie = false,
                            Niveau = NiveauAnomalie.Info,
                            Message = $"Modèle ancien ({row.Modele}) en {row.SousEtat} — envisager Revalorisation",
                            SousEtatConseille = "Revalorisation Dclass"
                        };
                    }
                }
            }

            ValidationHelpers.ApplyDefectCommentCheck(result, row);
            return result;
        }
    }
}
