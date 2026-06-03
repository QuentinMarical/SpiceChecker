using System;

namespace SpiceChecker.Models
{
    public class HardwareRow
    {
        public string AssetTag { get; set; } = "";
        public string Etat { get; set; } = "";
        public string SousEtat { get; set; } = "";
        public string Entrepot { get; set; } = "";
        public string CategorieModele { get; set; } = "";
        public string Modele { get; set; } = "";
        public string Fabricant { get; set; } = "";
        public double? RamGo { get; set; }
        public string NumeroSerie { get; set; } = "";
        public string AffecteA { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? DateChangementSousEtat { get; set; }
        public DateTime? DateRenouvellement { get; set; }
        public DateTime? DateSousEtat { get; set; }

        // Résultats du moteur de règles
        public string AnomalieMessage { get; set; } = "";
        public string AnomalieNiveau { get; set; } = "OK"; // OK | Info | Avertissement | Erreur
        public string SousEtatConseille { get; set; } = "";
    }
}