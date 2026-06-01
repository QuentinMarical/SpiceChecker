using System.Collections.Generic;
using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    public class RuleEngine
    {
        private readonly List<IRule> _rules = new List<IRule>();

        public RuleEngine()
        {
            _rules.Add(new HighRamLenovoRule());
            // Ajouter ici les futures règles :
            // _rules.Add(new DefectiveStateRule());
            // _rules.Add(new RevalorisationRule());
        }

        public void EvaluateRow(HardwareRow row)
        {
            row.AnomalieMessage = "";
            row.AnomalieNiveau = "OK";
            row.SousEtatConseille = "";

            foreach (var rule in _rules)
            {
                var result = rule.Evaluate(row);
                if (result.EstAnomalie)
                {
                    row.AnomalieMessage = result.Message;
                    row.AnomalieNiveau = result.Niveau.ToString();
                    row.SousEtatConseille = result.SousEtatConseille;
                    return; // première règle qui matche → on s'arrête
                }
            }
        }

        public void EvaluateAll(IEnumerable<HardwareRow> rows)
        {
            foreach (var row in rows)
                EvaluateRow(row);
        }
    }
}