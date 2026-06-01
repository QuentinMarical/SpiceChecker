using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Lenovo portable avec RAM >= 32 Go en "Re-use" ou "Disponible neuf" => anomalie à valider.
    /// </summary>
    public class HighRamLenovoRule : IRule
    {
        public string Nom => "Lenovo haute RAM";

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            var fabricant = (row.Fabricant ?? "").ToLowerInvariant();
            var modele = (row.Modele ?? "").ToLowerInvariant();
            var sousEtat = (row.SousEtat ?? "").ToLowerInvariant();

            bool estLenovo = fabricant.Contains("lenovo")
                          || modele.Contains("lenovo")
                          || modele.Contains("thinkpad")
                          || modele.Contains("thinkbook");

            bool ramElevee = row.RamGo.HasValue && row.RamGo.Value >= 32;

            bool sousEtatVise = sousEtat.Contains("re-use")
                             || sousEtat.Contains("reuse")
                             || sousEtat.Contains("disponible neuf")
                             || sousEtat.Contains("dispo neuf");

            if (estLenovo && ramElevee && sousEtatVise)
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