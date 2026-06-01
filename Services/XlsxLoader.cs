using ClosedXML.Excel;
using SpiceChecker.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpiceChecker.Services
{
    public class XlsxLoadResult
    {
        public List<HardwareRow> Rows { get; set; } = new List<HardwareRow>();
        public List<string> Headers { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string SourceFile { get; set; } = "";
        public string SheetName { get; set; } = "";
    }

    public class XlsxLoader
    {
        // Synonymes d'en-têtes acceptés (clé normalisée -> propriété cible)
        private static readonly Dictionary<string, string> HeaderMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // AssetTag
            { "etiquette",            "AssetTag" },
            { "etiquetteasset",       "AssetTag" },
            { "assettag",             "AssetTag" },
            { "tag",                  "AssetTag" },
            { "numeroinventaire",     "AssetTag" },

            // Etat
            { "etat",                 "Etat" },
            { "state",                "Etat" },
            { "statut",               "Etat" },

            // SousEtat
            { "sousetat",             "SousEtat" },
            { "substate",             "SousEtat" },
            { "substatus",            "SousEtat" },

            // Entrepot
            { "entrepot",             "Entrepot" },
            { "stockroom",            "Entrepot" },
            { "magasin",              "Entrepot" },

            // Categorie
            { "categoriedemodele",    "CategorieModele" },
            { "categoriemodele",      "CategorieModele" },
            { "categorie",            "CategorieModele" },
            { "modelcategory",        "CategorieModele" },

            // Modele
            { "modele",               "Modele" },
            { "model",                "Modele" },

            // Fabricant
            { "fabricant",            "Fabricant" },
            { "manufacturer",         "Fabricant" },
            { "marque",               "Fabricant" },

            // RAM
            { "ram",                  "RamGo" },
            { "ramgo",                "RamGo" },
            { "memoire",              "RamGo" },
            { "memory",               "RamGo" },

            // N° série
            { "numeroserie",          "NumeroSerie" },
            { "ns",                   "NumeroSerie" },
            { "serial",               "NumeroSerie" },
            { "serialnumber",         "NumeroSerie" },

            // Affecté à
            { "affectea",             "AffecteA" },
            { "assignedto",           "AffecteA" },
            { "utilisateur",          "AffecteA" },

            // Date changement sous-état
            { "datechangementsousetat", "DateChangementSousEtat" },
            { "datedernierchangementsousetat", "DateChangementSousEtat" },
            { "lastsubstatechange",   "DateChangementSousEtat" },
        };

        public XlsxLoadResult Load(string path)
        {
            var result = new XlsxLoadResult { SourceFile = path };

            if (!File.Exists(path))
                throw new FileNotFoundException("Fichier introuvable", path);

            using (var wb = new XLWorkbook(path))
            {
                // Premier onglet non vide
                var ws = wb.Worksheets.FirstOrDefault(s => !s.IsEmpty())
                         ?? wb.Worksheet(1);
                result.SheetName = ws.Name;

                var used = ws.RangeUsed();
                if (used == null)
                {
                    result.Warnings.Add("Feuille vide");
                    return result;
                }

                var rows = used.RowsUsed().ToList();
                if (rows.Count < 2)
                {
                    result.Warnings.Add("Pas de données (en-tête uniquement)");
                    return result;
                }

                // Ligne d'en-tête
                var headerRow = rows[0];
                var headerCells = headerRow.CellsUsed().ToList();
                var columnMapping = new Dictionary<int, string>(); // colNum -> propertyName

                foreach (var cell in headerCells)
                {
                    var raw = cell.GetString();
                    result.Headers.Add(raw);
                    var norm = Normalize(raw);
                    if (HeaderMap.TryGetValue(norm, out var prop))
                        columnMapping[cell.Address.ColumnNumber] = prop;
                }

                if (columnMapping.Count == 0)
                    result.Warnings.Add("Aucune colonne reconnue — vérifie les en-têtes du fichier");

                // Lignes de données
                for (int i = 1; i < rows.Count; i++)
                {
                    var r = rows[i];
                    var hw = new HardwareRow();
                    bool nonVide = false;

                    foreach (var kv in columnMapping)
                    {
                        var cell = r.Cell(kv.Key);
                        if (cell == null || cell.IsEmpty()) continue;
                        nonVide = true;
                        AssignValue(hw, kv.Value, cell);
                    }

                    if (nonVide) result.Rows.Add(hw);
                }
            }

            return result;
        }

        private static void AssignValue(HardwareRow hw, string prop, IXLCell cell)
        {
            switch (prop)
            {
                case "AssetTag": hw.AssetTag = cell.GetString().Trim(); break;
                case "Etat": hw.Etat = cell.GetString().Trim(); break;
                case "SousEtat": hw.SousEtat = cell.GetString().Trim(); break;
                case "Entrepot": hw.Entrepot = cell.GetString().Trim(); break;
                case "CategorieModele": hw.CategorieModele = cell.GetString().Trim(); break;
                case "Modele": hw.Modele = cell.GetString().Trim(); break;
                case "Fabricant": hw.Fabricant = cell.GetString().Trim(); break;
                case "NumeroSerie": hw.NumeroSerie = cell.GetString().Trim(); break;
                case "AffecteA": hw.AffecteA = cell.GetString().Trim(); break;
                case "RamGo": hw.RamGo = ParseRam(cell); break;
                case "DateChangementSousEtat":
                    hw.DateChangementSousEtat = ParseDate(cell);
                    break;
            }
        }

        private static double? ParseRam(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
                return cell.GetDouble();

            var s = cell.GetString();
            if (string.IsNullOrWhiteSpace(s)) return null;

            // Extrait le premier nombre (ex: "32 Go", "32GB", "32,0")
            var m = Regex.Match(s.Replace(',', '.'), @"-?\d+(\.\d+)?");
            if (m.Success && double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }

        private static DateTime? ParseDate(IXLCell cell)
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            var s = cell.GetString();
            if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var d))
                return d;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;
            return null;
        }

        /// <summary>
        /// Normalise un en-tête : minuscules, suppression accents, espaces, ponctuation.
        /// </summary>
        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var formD = s.Normalize(NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            return Regex.Replace(clean, @"[^a-z0-9]", "");
        }
    }
}