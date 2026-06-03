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
        // Les clés sont en minuscules, sans accents, sans espaces, sans tirets/ponctuation
        // IMPORTANT : la clé "etat" est gérée dynamiquement dans ResolveMapping pour éviter
        //             le conflit avec SousEtat dans certains exports SPICE.
        private static readonly Dictionary<string, string> HeaderMap =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // AssetTag
            { "etiquette",                    "AssetTag" },
            { "etiquetteasset",               "AssetTag" },
            { "assettag",                     "AssetTag" },
            { "tag",                          "AssetTag" },
            { "numeroinventaire",             "AssetTag" },
            { "elementdeconfiguration",       "AssetTag" },
            { "configelement",                "AssetTag" },

            // N° série
            { "numeroserie",                  "NumeroSerie" },
            { "ns",                           "NumeroSerie" },
            { "serial",                       "NumeroSerie" },
            { "serialnumber",                 "NumeroSerie" },

            // Affecté à
            { "affectea",                     "AffecteA" },
            { "assignedto",                   "AffecteA" },
            { "utilisateur",                  "AffecteA" },
            { "affectation",                  "AffecteA" },
            { "user",                         "AffecteA" },

            // Etat  ("etat" seul est résolu dynamiquement dans ResolveMapping)
            { "etat",                         "Etat" },
            { "state",                        "Etat" },
            { "statut",                       "Etat" },
            { "etatglobal",                   "Etat" },
            { "etatmateriel",                 "Etat" },

            // SousEtat
            { "sousetat",                     "SousEtat" },
            { "substate",                     "SousEtat" },
            { "substatus",                    "SousEtat" },
            { "repriseenattente",             "SousEtat" },

            // Entrepot
            { "entrepot",                     "Entrepot" },
            { "stockroom",                    "Entrepot" },
            { "magasin",                      "Entrepot" },
            { "emplacement",                  "Entrepot" },
            { "location",                     "Entrepot" },
            { "depot",                        "Entrepot" },
            { "warehouse",                    "Entrepot" },

            // Categorie
            { "categoriedemodele",            "CategorieModele" },
            { "categoriemodele",              "CategorieModele" },
            { "categorie",                    "CategorieModele" },
            { "modelcategory",                "CategorieModele" },
            { "categorymodel",                "CategorieModele" },
            { "category",                     "CategorieModele" },
            { "ordinateur",                   "CategorieModele" },

            // Modele
            { "modele",                       "Modele" },
            { "model",                        "Modele" },
            { "designation",                  "Modele" },

            // Fabricant
            { "fabricant",                    "Fabricant" },
            { "manufacturer",                 "Fabricant" },
            { "marque",                       "Fabricant" },
            { "methodeacquisition",           "Fabricant" },
            { "moyenneacquisition",           "Fabricant" },
            { "acquisitionmethod",            "Fabricant" },
            { "brand",                        "Fabricant" },
            { "constructeur",                 "Fabricant" },

            // RAM
            { "ram",                          "RamGo" },
            { "ramgo",                        "RamGo" },
            { "memoire",                      "RamGo" },
            { "memory",                       "RamGo" },

            // Date changement sous-état
            { "datechangementsousetat",       "DateChangementSousEtat" },
            { "datedernierchangementsousetat", "DateChangementSousEtat" },
            { "datesousetat",                 "DateChangementSousEtat" },
            { "lastsubstatechange",           "DateChangementSousEtat" },
            { "lastupdate",                   "DateChangementSousEtat" },
            { "datesubstate",                 "DateChangementSousEtat" },

            // Description / commentaires de panne
            { "description",                  "Description" },
            { "commentaire",                  "Description" },
            { "commentaires",                 "Description" },

            // Non mappé (colonnes à ignorer)
            { "justification",                "" },
            { "expirationgarantie",           "" },
            { "reservedpour",                 "" },
            { "centredcouts",                 "" },
            { "region",                       "" },
            { "datemiseadisposition",         "" },
            { "dateinventairephysique",       "" },
            { "daterenouvellementmateriel",   "" },
            { "datedernieretat",              "" },
            { "organisation",                 "" },
            { "organization",                 "" },
            { "dateerenouvellement",          "" },
            { "dateerenouvellementmateriel",  "" },
            { "daterenouvellement",           "" },
            { "renewaldate",                  "" },
        };

        // Clés du HeaderMap triées par longueur décroissante (pour le fallback partiel)
        private static readonly IReadOnlyList<KeyValuePair<string, string>> HeaderMapByLength =
            HeaderMap.OrderByDescending(kv => kv.Key.Length).ToList();

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

                // Première passe : collecter tous les headers normalisés présents dans le fichier
                var allNormalized = new HashSet<string>(
                    headerCells.Select(c => Normalize(c.GetString())),
                    StringComparer.Ordinal);

                foreach (var cell in headerCells)
                {
                    var raw = cell.GetString();
                    result.Headers.Add(raw);
                    var norm = Normalize(raw);
                    var alreadyMapped = new HashSet<string>(columnMapping.Values, StringComparer.Ordinal);
                    var prop = ResolveMapping(norm, alreadyMapped, allNormalized);
                    if (!string.IsNullOrEmpty(prop))
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

                    if (nonVide)
                    {
                        ExtractFromModele(hw);
                        result.Rows.Add(hw);
                    }
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
                case "Description": hw.Description = cell.GetString().Trim(); break;
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

        private static void ExtractFromModele(HardwareRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Modele)) return;

            var modele = row.Modele.ToUpperInvariant();

            // -- FABRICANT --
            if (string.IsNullOrWhiteSpace(row.Fabricant) || row.Fabricant == "?")
            {
                if (modele.Contains("LENOVO") || modele.Contains("THINKPAD") ||
                    modele.Contains("THINKBOOK") || modele.Contains("LEGION"))
                    row.Fabricant = "LENOVO";
                else if (modele.Contains("DELL") || modele.Contains("LATITUDE") ||
                         modele.Contains("XPS") || modele.Contains("OPTIPLEX") ||
                         modele.Contains("PRECISION"))
                    row.Fabricant = "DELL";
                else if (modele.Contains("HP") || modele.Contains("ELITEBOOK") ||
                         modele.Contains("PROBOOK") || modele.Contains("ZBOOK"))
                    row.Fabricant = "HP";
                else if (modele.Contains("ASUS"))
                    row.Fabricant = "ASUS";
                else if (modele.Contains("SAMSUNG"))
                    row.Fabricant = "SAMSUNG";
                else if (modele.Contains("ACER"))
                    row.Fabricant = "ACER";
                else if (modele.Contains("FUJITSU"))
                    row.Fabricant = "FUJITSU";
                else if (modele.Contains("PANASONIC"))
                    row.Fabricant = "PANASONIC";
            }

            // -- RAM depuis modele (suffixe type "... I5 8", "... I7 32") --
            if (!row.RamGo.HasValue)
            {
                var ramMatch = System.Text.RegularExpressions.Regex.Match(
                    row.Modele,
                    @"\b(I3|I5|I7|I9|R3|R5|R7|R9)\s+(\d{1,2})\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (ramMatch.Success &&
                    double.TryParse(ramMatch.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var ramVal) &&
                    ramVal > 0 && ramVal <= 128)
                    row.RamGo = ramVal;
            }
        }

        /// <summary>
        /// Résout le mapping d'un header normalisé vers un nom de propriété.
        /// Match exact en priorité, puis fallback partiel (clé la plus longue d'abord).
        /// Gère l'ambiguïté « etat » : si SousEtat est couvert par une autre colonne,
        /// « etat » seul est interprété comme Etat ; sinon comme SousEtat (compatibilité SPICE).
        /// </summary>
        private static string ResolveMapping(
            string normalizedHeader,
            HashSet<string> alreadyMappedProps,
            HashSet<string> allNormalizedHeaders)
        {
            // Match exact
            if (HeaderMap.TryGetValue(normalizedHeader, out var propName))
            {
                // Ambiguïté : "etat" peut désigner Etat ou SousEtat selon le fichier
                if (normalizedHeader == "etat")
                {
                    bool sousEtatCoveredByOtherColumn =
                        alreadyMappedProps.Contains("SousEtat") ||
                        allNormalizedHeaders.Any(h => h != "etat" && HeaderMap.TryGetValue(h, out var p) && p == "SousEtat");
                    return sousEtatCoveredByOtherColumn ? "Etat" : "SousEtat";
                }
                return propName;
            }

            // Fallback partiel : clé la plus longue (>= 4 chars) contenue dans le header
            foreach (var kv in HeaderMapByLength)
            {
                if (kv.Key.Length >= 4 && normalizedHeader.Contains(kv.Key))
                    return kv.Value;
            }

            return string.Empty;
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