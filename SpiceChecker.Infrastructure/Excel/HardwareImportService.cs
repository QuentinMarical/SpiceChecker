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
        DateDerniereModifSousEtat
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

            var asset = new HardwareAsset
            {
                AssetTag = GetValue(rowValues, ColumnKind.AssetTag),
                Categorie = ParseCategorie(GetValue(rowValues, ColumnKind.Categorie)),
                Fabricant = GetValue(rowValues, ColumnKind.Fabricant),
                Modele = GetValue(rowValues, ColumnKind.Modele),
                RamGo = ParseRam(GetValue(rowValues, ColumnKind.RamGo)),
                DateAcquisition = ParseDate(rowValues, ColumnKind.DateAcquisition),
                DateRenouvellement = ParseDate(rowValues, ColumnKind.DateRenouvellement),
                DateDerniereModifSousEtat = ParseDate(rowValues, ColumnKind.DateDerniereModifSousEtat),
                SousEtat = ParseSousEtat(GetValue(rowValues, ColumnKind.SousEtat)),
                Commentaire = GetValue(rowValues, ColumnKind.Commentaire)
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
            for (var col = firstCol; col <= lastCol; col++)
            {
                var text = Normalize(worksheet.Cell(row, col).GetString());
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (text.Contains("asset", StringComparison.Ordinal)
                    || text.Contains("tag", StringComparison.Ordinal)
                    || text.Contains("serie", StringComparison.Ordinal)
                    || text.Contains("matricule", StringComparison.Ordinal))
                {
                    return row;
                }
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

        if (header.Contains("asset") || header.Contains("tag") || header.Contains("matricule")) return ColumnKind.AssetTag;
        if (header.Contains("categorie") || header.Contains("category")) return ColumnKind.Categorie;
        if (header is "fab" || header.Contains("fabricant") || header.Contains("manufacturer") || header.Contains("marque")) return ColumnKind.Fabricant;
        if (header.Contains("modele") || header.Contains("model")) return ColumnKind.Modele;
        if (header.Contains("ram") || header.Contains("memoire") || header.Contains("memory")) return ColumnKind.RamGo;

        if (header.Contains("renouv") || header.Contains("daterenouvellement") || header.Contains("datedefin") || header.Contains("renewal"))
            return ColumnKind.DateRenouvellement;

        if (header.Contains("acquisition") || header.Contains("achat"))
            return ColumnKind.DateAcquisition;

        if (header.Contains("sousetat") || header.Contains("substate") || header.Contains("substatus"))
            return ColumnKind.SousEtat;

        if (header.Contains("commentaire") || header.Contains("description") || header.Contains("comment"))
            return ColumnKind.Commentaire;

        if (header.Contains("datemodif")
            || header.Contains("datedernieremodifsousetat")
            || header.Contains("datechangement")
            || (header.Contains("date") && header.Contains("modif")))
            return ColumnKind.DateDerniereModifSousEtat;

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

        var match = Regex.Match(value, @"\d+");
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ram)
            ? ram
            : null;
    }

    private static CategorieEquipement ParseCategorie(string value)
    {
        var normalized = Normalize(value);

        if (normalized.Contains("ordinateur") || normalized.Contains("pc") || normalized.Contains("laptop") || normalized.Contains("notebook"))
            return CategorieEquipement.Ordinateur;

        if (normalized.Contains("reseau") || normalized.Contains("network") || normalized.Contains("switch") || normalized.Contains("routeur"))
            return CategorieEquipement.EquipementReseau;

        if (normalized.Contains("peripher") || normalized.Contains("peripheral") || normalized.Contains("ecran") || normalized.Contains("clavier") || normalized.Contains("souris") || normalized.Contains("imprimante"))
            return CategorieEquipement.Peripherique;

        return CategorieEquipement.Autre;
    }

    private static SousEtat ParseSousEtat(string value)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
            return SousEtat.Autre;

        if (normalized.Contains("disponibleneuf") || normalized == "neuf")
            return SousEtat.DisponibleNeuf;

        if (normalized.Contains("repriseenattente"))
            return SousEtat.RepriseEnAttente;

        if (normalized.Contains("revalorisation") || normalized.Contains("dclass") || normalized.Contains("retourloueur"))
            return SousEtat.Revalorisation;

        if (normalized.Contains("defectueux") || normalized.Contains("defect") || normalized.Contains("hs") || normalized.Contains("panne") || normalized.Contains("casse"))
            return SousEtat.Defectueux;

        if (normalized.Contains("reparation") || normalized.Contains("reparer"))
            return SousEtat.EnReparation;

        if (normalized.Contains("disponible") || normalized.Contains("reuse") || normalized.Contains("reuse") || normalized.Contains("dispo"))
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
