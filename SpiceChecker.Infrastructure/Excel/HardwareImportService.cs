using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SpiceChecker.Application.Services;
using SpiceChecker.Domain.Entities;
using SpiceChecker.Domain.Enums;

namespace SpiceChecker.Infrastructure.Excel;

/// <summary>
/// Service d'import Excel robuste pour les équipements matériels.
/// </summary>
public sealed class HardwareImportService : IHardwareImportService
{
    private static readonly CultureInfo FrCulture = new("fr-FR");

    private enum ColumnKind
    {
        None,
        AssetTag,
        Categorie,
        Fabricant,
        Modele,
        RamGo,
        DateAcquisition,
        DateRenouvellement,
        SousEtat,
        Commentaire,
        DateDerniereModifSousEtat,
        Etat,
        Entrepot
    }

    public async Task<IReadOnlyList<HardwareAsset>> ImportAsync(
        Stream excelStream,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var assets = new List<HardwareAsset>();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => !w.IsEmpty()) ?? workbook.Worksheet(1);
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return assets.AsReadOnly();
        }

        var firstRow = usedRange.RangeAddress.FirstAddress.RowNumber;
        var lastRow = usedRange.RangeAddress.LastAddress.RowNumber;
        var firstCol = usedRange.RangeAddress.FirstAddress.ColumnNumber;
        var lastCol = usedRange.RangeAddress.LastAddress.ColumnNumber;

        var headerRow = FindHeaderRow(worksheet, firstRow, lastRow, firstCol, lastCol);
        if (headerRow is null)
        {
            return assets.AsReadOnly();
        }

        var mappedColumns = BuildColumnMapping(worksheet, headerRow.Value, firstCol, lastCol);

        for (var rowNumber = headerRow.Value + 1; rowNumber <= lastRow; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowValues = new Dictionary<ColumnKind, string>();
            var isEmptyDataRow = true;

            foreach (var kvp in mappedColumns)
            {
                var cell = worksheet.Cell(rowNumber, kvp.Key);
                var rawText = ReadCellText(cell);
                rowValues[kvp.Value] = rawText;

                if (!string.IsNullOrWhiteSpace(rawText))
                {
                    isEmptyDataRow = false;
                }
            }

            if (isEmptyDataRow)
            {
                continue;
            }

            var fabricant = GetValue(rowValues, ColumnKind.Fabricant);
            var modele = GetValue(rowValues, ColumnKind.Modele);
            var ramGo = ParseRam(GetValue(rowValues, ColumnKind.RamGo));
            NormalizeImportedFields(ref fabricant, ref modele, ref ramGo);

            var etatValue = GetValue(rowValues, ColumnKind.Etat);
            var entrepotValue = GetValue(rowValues, ColumnKind.Entrepot);
            var sousEtatValue = GetEffectiveSousEtat(GetValue(rowValues, ColumnKind.SousEtat), etatValue);
            var commentaire = BuildCommentaire(GetValue(rowValues, ColumnKind.Commentaire), etatValue, entrepotValue);

            var asset = new HardwareAsset
            {
                AssetTag = GetValue(rowValues, ColumnKind.AssetTag),
                Categorie = ParseCategorie(GetValue(rowValues, ColumnKind.Categorie)),
                Fabricant = fabricant,
                Modele = modele,
                RamGo = ramGo,
                DateAcquisition = ParseDate(rowValues, ColumnKind.DateAcquisition),
                DateRenouvellement = ParseDate(rowValues, ColumnKind.DateRenouvellement),
                DateDerniereModifSousEtat = ParseDate(rowValues, ColumnKind.DateDerniereModifSousEtat),
                SousEtat = ParseSousEtat(sousEtatValue),
                Commentaire = commentaire
            };

            assets.Add(asset);

            if (assets.Count % 100 == 0)
            {
                progress?.Report($"Ligne {rowNumber} traitée...");
            }
        }

        return assets.AsReadOnly();
    }

    private static int? FindHeaderRow(IXLWorksheet worksheet, int firstRow, int lastRow, int firstCol, int lastCol)
    {
        for (var row = firstRow; row <= lastRow; row++)
        {
            var recognizedColumns = 0;
            var hasAssetColumn = false;

            for (var col = firstCol; col <= lastCol; col++)
            {
                var text = Normalize(worksheet.Cell(row, col).GetString());
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var kind = ResolveColumnKind(text);
                if (kind == ColumnKind.None)
                {
                    continue;
                }

                recognizedColumns++;
                if (kind == ColumnKind.AssetTag)
                {
                    hasAssetColumn = true;
                }
            }

            if (hasAssetColumn || recognizedColumns >= 3)
            {
                return row;
            }
        }

        return null;
    }

    private static Dictionary<int, ColumnKind> BuildColumnMapping(IXLWorksheet worksheet, int headerRow, int firstCol, int lastCol)
    {
        var mapping = new Dictionary<int, ColumnKind>();

        for (var col = firstCol; col <= lastCol; col++)
        {
            var normalizedHeader = Normalize(worksheet.Cell(headerRow, col).GetString());
            var kind = ResolveColumnKind(normalizedHeader);
            if (kind != ColumnKind.None)
            {
                mapping[col] = kind;
            }
        }

        return mapping;
    }

    private static ColumnKind ResolveColumnKind(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return ColumnKind.None;
        }

        if (header.Contains("asset")
            || header.Contains("tag")
            || header.Contains("matricule")
            || header.Contains("etiquette")
            || header.Contains("numeroinventaire")
            || header.Contains("elementdeconfiguration")
            || header.Contains("configelement"))
            return ColumnKind.AssetTag;

        if (header.Contains("categoriedemodele")
            || header.Contains("categoriemodele")
            || header.Contains("categorie")
            || header.Contains("categorymodel")
            || header.Contains("modelcategory")
            || header.Contains("category"))
            return ColumnKind.Categorie;

        if (header is "fab"
            || header.Contains("fabricant")
            || header.Contains("manufacturer")
            || header.Contains("marque")
            || header.Contains("constructeur"))
            return ColumnKind.Fabricant;

        if (header.Contains("entrepot") || header.Contains("stockroom") || header.Contains("magasin") || header.Contains("warehouse") || header.Contains("depot"))
            return ColumnKind.Entrepot;

        if (header.Contains("modele") || header.Contains("model") || header.Contains("designation"))
            return ColumnKind.Modele;

        if (header.Contains("ram") || header.Contains("memoire") || header.Contains("memory"))
            return ColumnKind.RamGo;

        if (header.Contains("renouv")
            || header.Contains("daterenouvellement")
            || header.Contains("daterenouvellementmateriel")
            || header.Contains("datedefin")
            || header.Contains("expirationgarantie")
            || header.Contains("renewaldate")
            || header.Contains("renewal"))
            return ColumnKind.DateRenouvellement;

        if (header.Contains("datechangementsousetat")
            || header.Contains("datedernierchangementsousetat")
            || header.Contains("datesousetat")
            || header.Contains("lastsubstatechange")
            || header.Contains("datesubstate")
            || header.Contains("lastupdate")
            || header.Contains("datemodif")
            || header.Contains("datedernieremodifsousetat")
            || header.Contains("datedernieretat")
            || header.Contains("datechangement")
            || (header.Contains("date") && header.Contains("modif")))
            return ColumnKind.DateDerniereModifSousEtat;

        if (header.Contains("acquisition") || header.Contains("achat"))
            return ColumnKind.DateAcquisition;

        if (header == "etat" || header.Contains("state") || header.Contains("statut"))
            return ColumnKind.Etat;

        if (header.Contains("sousetat")
            || header.Contains("substate")
            || header.Contains("substatus"))
            return ColumnKind.SousEtat;

        if (header.Contains("commentaire") || header.Contains("commentaires") || header.Contains("description") || header.Contains("comment"))
            return ColumnKind.Commentaire;

        return ColumnKind.None;
    }

    private static string ReadCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            return dateTime.ToString("dd/MM/yyyy", FrCulture);
        }

        var formatted = cell.GetFormattedString();
        if (!string.IsNullOrWhiteSpace(formatted))
        {
            return formatted.Trim();
        }

        return cell.GetString().Trim();
    }

    private static DateTime? ParseDate(IReadOnlyDictionary<ColumnKind, string> rowValues, ColumnKind kind)
    {
        var value = GetValue(rowValues, kind);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, FrCulture, DateTimeStyles.None, out var frDate))
        {
            return frDate;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariantDate))
        {
            return invariantDate;
        }

        return null;
    }

    private static int? ParseRam(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = Regex.Match(value, @"(?i)\b(?<ram>4|8|12|16|24|32|48|64|96|128|192|256|384|512|768|1024)\s*(go|gb|g)\b");
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups["ram"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ram)
            ? ram
            : null;
    }

    private static void NormalizeImportedFields(ref string fabricant, ref string modele, ref int? ramGo)
    {
        if (string.IsNullOrWhiteSpace(modele))
        {
            return;
        }

        var knownManufacturers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LENOVO"] = "Lenovo",
            ["DELL"] = "Dell",
            ["HP"] = "HP",
            ["ARUBA"] = "Aruba",
            ["CISCO"] = "Cisco",
            ["JUNIPER"] = "Juniper",
            ["FORTINET"] = "Fortinet",
            ["UBIQUITI"] = "Ubiquiti",
            ["MICROSOFT"] = "Microsoft",
            ["ASUS"] = "Asus",
            ["SAMSUNG"] = "Samsung",
            ["ACER"] = "Acer",
            ["FUJITSU"] = "Fujitsu",
            ["PANASONIC"] = "Panasonic"
        };

        var trimmedModele = modele.Trim();
        foreach (var manufacturer in knownManufacturers)
        {
            if (!trimmedModele.StartsWith(manufacturer.Key + " ", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(trimmedModele, manufacturer.Key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(fabricant))
            {
                fabricant = manufacturer.Value;
            }

            trimmedModele = trimmedModele[manufacturer.Key.Length..].TrimStart('-', ' ', '/', '\\');
            if (string.IsNullOrWhiteSpace(trimmedModele))
            {
                trimmedModele = modele.Trim();
            }

            break;
        }

        if (!ramGo.HasValue)
        {
            var explicitRamMatch = Regex.Match(trimmedModele, @"(?i)\b(?<ram>4|8|12|16|24|32|48|64|96|128|192|256|384|512|768|1024)\s*(go|gb|g)\b");
            if (explicitRamMatch.Success
                && int.TryParse(explicitRamMatch.Groups["ram"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var explicitRam)
                && explicitRam > 0
                && explicitRam <= 1024)
            {
                ramGo = explicitRam;
            }
        }

        if (!ramGo.HasValue)
        {
            var cpuRamMatch = Regex.Match(trimmedModele, @"\b(I3|I5|I7|I9|R3|R5|R7|R9)\s+(8|12|16|24|32|48|64)\b", RegexOptions.IgnoreCase);
            if (cpuRamMatch.Success
                && int.TryParse(cpuRamMatch.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cpuRam)
                && cpuRam > 0
                && cpuRam <= 128)
            {
                var storageSuffix = trimmedModele[(cpuRamMatch.Index + cpuRamMatch.Length)..].Trim();
                if (string.IsNullOrWhiteSpace(storageSuffix)
                    || !Regex.IsMatch(storageSuffix, @"(?i)^\d+\s*(go|gb|g|to|tb)\b"))
                {
                    ramGo = cpuRam;
                }
            }
        }

        trimmedModele = Regex.Replace(trimmedModele, @"\s{2,}", " ").Trim(' ', '-', '/', '\\', '_');

        modele = trimmedModele;
    }

    private static CategorieEquipement ParseCategorie(string value)
    {
        var normalized = Normalize(value);

        if (normalized.Contains("ordinateur") || normalized.Contains("pc") || normalized.Contains("laptop") || normalized.Contains("notebook"))
            return CategorieEquipement.Ordinateur;

        if (normalized.Contains("equipementreseau") || normalized.Contains("reseau") || normalized.Contains("network") || normalized.Contains("switch") || normalized.Contains("routeur"))
            return CategorieEquipement.EquipementReseau;

        return CategorieEquipement.Serveur;
    }

    private static string GetEffectiveSousEtat(string sousEtat, string etat)
    {
        if (!string.IsNullOrWhiteSpace(sousEtat))
        {
            return sousEtat;
        }

        return etat;
    }

    private static string BuildCommentaire(string commentaire, string etat, string entrepot)
    {
        if (!string.IsNullOrWhiteSpace(commentaire))
        {
            return commentaire;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(etat))
        {
            parts.Add($"État: {etat}");
        }

        if (!string.IsNullOrWhiteSpace(entrepot))
        {
            parts.Add($"Entrepôt: {entrepot}");
        }

        return string.Join(" | ", parts);
    }

    private static SousEtat ParseSousEtat(string value)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
            return SousEtat.Autre;

        if (normalized.Contains("disponibleneuf") || normalized == "neuf")
            return SousEtat.DisponibleNeuf;

        if (normalized.Contains("reserve") || normalized.Contains("masterise"))
            return SousEtat.RepriseEnAttente;

        if (normalized.Contains("repriseenattente"))
            return SousEtat.RepriseEnAttente;

        if (normalized.Contains("revalorisation") || normalized.Contains("dclass") || normalized.Contains("retourloueur"))
            return SousEtat.Revalorisation;

        if (normalized.Contains("defectueux") || normalized.Contains("defect") || normalized.Contains("hs") || normalized.Contains("panne") || normalized.Contains("casse"))
            return SousEtat.Defectueux;

        if (normalized.Contains("reparation") || normalized.Contains("reparer"))
            return SousEtat.EnReparation;

        if (normalized.Contains("disponible") || normalized.Contains("reuse") || normalized.Contains("enstock") || normalized.Contains("dispo"))
            return SousEtat.Disponible;

        return SousEtat.Autre;
    }

    private static string GetValue(IReadOnlyDictionary<ColumnKind, string> rowValues, ColumnKind key)
    {
        return rowValues.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);

        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var noAccents = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        return Regex.Replace(noAccents, @"[^a-z0-9]", string.Empty);
    }
}
