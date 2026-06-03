using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle 1 — Lenovo portable avec RAM >= ³² Go en statut "disponible" (Re-Use, Disponible neuf,
    /// ou équivalent). Anomalie si non validé par l'asset management.
    /// Sous-états "disponibles" pris en compte : Disponible Re-Use, Disponible neuf, Dispo neuf,
    /// Re-use, Reprise en attente (statut transitoire acceptable mais à surveiller).
    /// </summary>
    public class HighRamLenovoRule : IRule
    {
        public string Nom => "Lenovo haute RAM";

        private static readonly string[] _sousEtatsViseE = new[]
        {
            "Disponible Re-Use", "Disponible neuf", "Dispo neuf",
            "Re-use", "Reuse", "Reprise en attente"
        };

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            var fabricant = (row.Fabricant ?? "").ToLowerInvariant();
            var modele    = (row.Modele    ?? "").ToLowerInvariant();
            var sousEtat  = (row.SousEtat  ?? "").ToLowerInvariant();

            // Est-ce un Lenovo (ThinkPad, ThinkBook, Legion comptent)
            bool estLenovo = fabricant.Contains("lenovo")
                          || modele.Contains("lenovo")
                          || modele.Contains("thinkpad")
                          || modele.Contains("thinkbook")
                          || modele.Contains("legion");

            bool ramElevee = row.RamGo.HasValue && row.RamGo.Value >= 32;

            bool sousEtatVisé = false;
            foreach (var se in _sousEtatsViseE)
                if (sousEtat.Contains(se.ToLowerInvariant())) { sousEtatVisé = true; break; }

            if (estLenovo && ramElevee && sousEtatVisé)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Avertissement,
                    Message = $"Lenovo {row.RamGo} Go en \"{row.SousEtat}\" — à valider (asset management)",
                    SousEtatConseille = "Réservé / À valider"
                };
            }

            return EvaluationResult.Ok();
        }
    }
}
