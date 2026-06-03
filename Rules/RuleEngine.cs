using System;
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
            _rules.Add(new DefectiveStateRule());
            _rules.Add(new RevalorisationRule());
            _rules.Add(new L13L14RenewalRule());  // ← AJOUT : Règle L13/L14 8 Go avec logique date renouvellement
            _rules.Add(new StaleSubstateRule());
        }

        public void EvaluateRow(HardwareRow row)
        {
            row.AnomalieMessage = "";
            row.AnomalieNiveau = "OK";
            row.SousEtatConseille = "";

            // Early exit : "Disponible neuf" et "Reprise en attente" ne sont JAMAIS des anomalies
            if (!string.IsNullOrEmpty(row.SousEtat) &&
                (row.SousEtat.Equals("Disponible neuf", StringComparison.OrdinalIgnoreCase) ||
                 row.SousEtat.Equals("Reprise en attente", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

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