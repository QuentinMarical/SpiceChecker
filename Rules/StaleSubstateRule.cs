using System;
using SpiceChecker.Models;

namespace SpiceChecker.Rules
{
    /// <summary>
    /// Règle 4 — Détecte les sous-états non mis à jour depuis un délai excessif.
    /// Seuil : > 90 jours sans changement = Avertissement
    ///         > 180 jours sans changement = Erreur
    /// Basé sur la colonne DateChangementSousEtat (ou datedernieretat si mappée).
    /// </summary>
    public class StaleSubstateRule : IRule
    {
        public string Nom => "Sous-état ancien";

        // Seuil d'alerte en jours — TJMR (> 90j = avertissement)
        private const int SeuilAvertissement = 90;
        // Seuil critique (> 180j = erreur)
        private const int SeuilErreur = 180;

        // Sous-états qu'on ne flag pas comme "anciens" (en attente légitime)
        private static readonly string[] _exclus = new[]
        {
            "En attente de don", "Réservé/Masterisé", "RservMasteris",
            "Réparation en attente", "Reprise en attente", "A blanchir"
        };

        public EvaluationResult Evaluate(HardwareRow row)
        {
            if (row == null) return EvaluationResult.Ok();

            // On ne vérifie que si une date est renseignée
            if (!row.DateChangementSousEtat.HasValue)
                return EvaluationResult.Ok();

            var sousEtat = (row.SousEtat ?? "").ToLowerInvariant();

            // Exclure les sous-états en attente légitime
            foreach (var ex in _exclus)
                if (sousEtat.Contains(ex.ToLowerInvariant()))
                    return EvaluationResult.Ok();

            var delta = DateTime.Now - row.DateChangementSousEtat.Value;
            int jours = (int)delta.TotalDays;

            if (jours > SeuilErreur)
            {
                return new EvaluationResult
                {
                    EstAnomalie = true,
                    Niveau = NiveauAnomalie.Erreur,
                    Message = $"Sous-état \"{row.SousEtat}\" inchangé depuis {jours} jours",
                    SousEtatConseille = "Mettre à jour le sous-état"
                };
            }
            else if (jours > SeuilAvertissement)
            {
                return new EvaluationResult
                {
                    EstAnomalie = false,
                    Niveau = NiveauAnomalie.Info,
                    Message = $"Sous-état \"{row.SousEtat}\" inchangé depuis {jours} jours",
                    SousEtatConseille = "Vérifier l'obsolescence"
                };
            }

            return EvaluationResult.Ok();
        }
    }
}
