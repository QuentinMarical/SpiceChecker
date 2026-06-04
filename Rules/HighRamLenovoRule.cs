using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle 1 — Override global : Lenovo 16/32 Go catégorie Ordinateur.
    /// Si IsAnomaly = false, les autres règles ne s'appliquent pas pour cette ligne.
    /// </summary>
    public class HighRamLenovoRule : IRule
    {
        public string Nom => "Lenovo haute RAM";
        public bool IsOverride => true;

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            bool estLenovo = row.Fabricant?.Contains("lenovo", StringComparison.OrdinalIgnoreCase) == true;
            bool ramCiblee = row.RamGo.HasValue && (row.RamGo.Value == 16 || row.RamGo.Value == 32);
            bool estOrdinateur = row.CategorieModele?.Contains("ordinateur", StringComparison.OrdinalIgnoreCase) == true;

            if (!estLenovo || !ramCiblee || !estOrdinateur)
                return EvaluationResult.Ok();

            bool estDefectueux = row.SousEtat?.Contains("défectueux", StringComparison.OrdinalIgnoreCase) == true;
            bool estRevalorisation = row.SousEtat?.Contains("revalorisation", StringComparison.OrdinalIgnoreCase) == true;

            if (estDefectueux)
            {
                return new EvaluationResult
                {
                    EstAnomalie = false,
                    Niveau = NiveauAnomalie.OK,
                    Message = $"Lenovo {row.RamGo} Go défectueux OK → conserver en Défectueux, jamais en Revalorisation"
                };
            }

            if (estRevalorisation)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Erreur,
                    Message = $"Lenovo {row.RamGo} Go ne doit jamais être en Revalorisation (matériel stratégique)",
                    SousEtatConseille = "Classer en Disponible Re-Use"
                };
            }

            // Fonctionnel, Re-Use, etc. → override : on bloque les règles suivantes
            return new EvaluationResult
            {
                EstAnomalie = false,
                Niveau = NiveauAnomalie.OK,
                Message = $"Lenovo {row.RamGo} Go OK → Disponible Re-Use"
            };
        }
    }
}
