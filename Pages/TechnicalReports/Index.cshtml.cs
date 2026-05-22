using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.TechnicalReports;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int RamThresholdGb { get; set; } = 8;

    [BindProperty(SupportsGet = true)]
    public bool ShowOnlyIssues { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IList<TechnicalAuditRow> Rows { get; set; } = new List<TechnicalAuditRow>();

    public int CandidateCount { get; set; }
    public int MissingSpecsCount { get; set; }
    public int LowRamCount { get; set; }
    public int HddOrUnknownStorageCount { get; set; }
    public int OldOsCount { get; set; }
    public int InteractiveWithoutOpsSpecsCount { get; set; }
    public int IssueCount { get; set; }

    public async Task OnGetAsync()
    {
        var items = await _db.InventoryItems
            .Where(x => x.IsActive)
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Include(x => x.TechnicalSpecs)
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .ToListAsync();

        var rows = new List<TechnicalAuditRow>();

        foreach (var item in items)
        {
            var categoryName = item.InventoryCategory?.Name ?? string.Empty;

            if (!IsTechnicalCandidate(item.Name, categoryName))
            {
                continue;
            }

            var specs = item.TechnicalSpecs;
            var hasSpecs = specs != null && specs.HasAnyValue();
            var ramGb = ExtractRamGb(specs?.MemoryRam);
            var lowRam = ramGb.HasValue && ramGb.Value < RamThresholdGb;
            var missingSpecs = !hasSpecs;

            var storageDescription = string.Join(" ",
                specs?.Storage ?? string.Empty,
                specs?.StorageType ?? string.Empty).Trim();

            var storageUnknown = string.IsNullOrWhiteSpace(storageDescription);
            var hddStorage = LooksLikeHdd(storageDescription);
            var hddOrUnknownStorage = storageUnknown || hddStorage;

            var oldOs = LooksLikeOldOperatingSystem(specs?.OperatingSystem);
            var isInteractive = IsInteractiveItem(item.Name, categoryName);
            var interactiveWithoutOpsSpecs = isInteractive && string.IsNullOrWhiteSpace(specs?.OpsModuleModel) && missingSpecs;

            var issues = new List<string>();

            if (missingSpecs)
            {
                issues.Add("Λείπουν τεχνικά χαρακτηριστικά");
            }

            if (lowRam)
            {
                issues.Add($"RAM κάτω από {RamThresholdGb}GB");
            }

            if (storageUnknown)
            {
                issues.Add("Άγνωστος δίσκος/αποθήκευση");
            }
            else if (hddStorage)
            {
                issues.Add("HDD / υποψήφιο για SSD");
            }

            if (oldOs)
            {
                issues.Add("Παλαιό λειτουργικό");
            }

            if (interactiveWithoutOpsSpecs)
            {
                issues.Add("Διαδραστικό χωρίς στοιχεία OPS/Mini PC");
            }

            var row = new TechnicalAuditRow
            {
                InventoryItemId = item.Id,
                RoomName = item.Room?.Name ?? "Χωρίς χώρο",
                CategoryName = categoryName,
                ItemName = item.Name,
                Brand = item.Brand ?? string.Empty,
                Model = item.Model ?? string.Empty,
                SerialNumber = item.SerialNumber ?? string.Empty,
                Processor = specs?.Processor ?? string.Empty,
                MemoryRam = specs?.MemoryRam ?? string.Empty,
                MemoryType = specs?.MemoryType ?? string.Empty,
                Storage = specs?.Storage ?? string.Empty,
                StorageType = specs?.StorageType ?? string.Empty,
                Graphics = specs?.Graphics ?? string.Empty,
                OperatingSystem = specs?.OperatingSystem ?? string.Empty,
                OpsModuleModel = specs?.OpsModuleModel ?? string.Empty,
                TechnicalNotes = specs?.TechnicalNotes ?? string.Empty,
                HasSpecs = hasSpecs,
                RamGb = ramGb,
                IsLowRam = lowRam,
                IsHddOrUnknownStorage = hddOrUnknownStorage,
                HasOldOs = oldOs,
                IsInteractiveWithoutOpsSpecs = interactiveWithoutOpsSpecs,
                Issues = issues
            };

            rows.Add(row);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();

            rows = rows
                .Where(x =>
                    x.RoomName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Model.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.SerialNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Processor.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.MemoryRam.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Storage.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.OperatingSystem.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.IssueText.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        CandidateCount = rows.Count;
        MissingSpecsCount = rows.Count(x => !x.HasSpecs);
        LowRamCount = rows.Count(x => x.IsLowRam);
        HddOrUnknownStorageCount = rows.Count(x => x.IsHddOrUnknownStorage);
        OldOsCount = rows.Count(x => x.HasOldOs);
        InteractiveWithoutOpsSpecsCount = rows.Count(x => x.IsInteractiveWithoutOpsSpecs);
        IssueCount = rows.Count(x => x.HasIssues);

        if (ShowOnlyIssues)
        {
            rows = rows.Where(x => x.HasIssues).ToList();
        }

        Rows = rows
            .OrderByDescending(x => x.HasIssues)
            .ThenBy(x => x.RoomName)
            .ThenBy(x => x.ItemName)
            .ToList();
    }


    public async Task<IActionResult> OnGetExportAsync()
    {
        await OnGetAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Τεχνικός έλεγχος");

        worksheet.Style.Font.FontName = "Segoe UI";

        var lastColumn = 17;

        worksheet.Cell(1, 1).Value = "School Inventory Manager";
        worksheet.Range(1, 1, 1, lastColumn).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 11;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0F766E");

        worksheet.Cell(2, 1).Value = "Αναφορά τεχνικού ελέγχου εξοπλισμού";
        worksheet.Range(2, 1, 2, lastColumn).Merge();
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 18;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#111827");

        worksheet.Cell(3, 1).Value = "Ημερομηνία εξαγωγής";
        worksheet.Cell(3, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        worksheet.Cell(3, 4).Value = "Φίλτρα";
        worksheet.Cell(3, 5).Value =
            (ShowOnlyIssues ? "Μόνο εγγραφές με θέματα" : "Όλες οι τεχνικές συσκευές") +
            (string.IsNullOrWhiteSpace(Search) ? string.Empty : $" | Αναζήτηση: {Search}");

        worksheet.Range(3, 1, 3, lastColumn).Style.Font.FontColor = XLColor.FromHtml("#374151");

        var summaryRow = 5;

        var summaryItems = new (string Label, int Value)[]
        {
            ("Τεχνικές συσκευές", CandidateCount),
            ("Με θέματα", IssueCount),
            ("Λείπουν specs", MissingSpecsCount),
            ($"RAM < {RamThresholdGb}GB", LowRamCount),
            ("HDD / άγνωστο", HddOrUnknownStorageCount),
            ("Παλαιό OS", OldOsCount)
        };

        var summaryColumn = 1;

        foreach (var summaryItem in summaryItems)
        {
            worksheet.Cell(summaryRow, summaryColumn).Value = summaryItem.Label;
            worksheet.Cell(summaryRow + 1, summaryColumn).Value = summaryItem.Value;

            var summaryRange = worksheet.Range(summaryRow, summaryColumn, summaryRow + 1, summaryColumn + 1);
            summaryRange.Merge();
            summaryRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            summaryRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#9CA3AF");
            summaryRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            summaryRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell(summaryRow, summaryColumn).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, summaryColumn).Style.Font.FontSize = 9;
            worksheet.Cell(summaryRow, summaryColumn).Style.Font.FontColor = XLColor.FromHtml("#374151");
            worksheet.Cell(summaryRow + 1, summaryColumn).Style.Font.Bold = true;
            worksheet.Cell(summaryRow + 1, summaryColumn).Style.Font.FontSize = 16;
            worksheet.Cell(summaryRow + 1, summaryColumn).Style.Font.FontColor = XLColor.FromHtml("#111827");

            summaryColumn += 2;
        }

        var headerRow = 8;
        var headers = new[]
        {
            "Α/Α",
            "Χώρος",
            "Κατηγορία",
            "Συσκευή",
            "Μάρκα",
            "Μοντέλο",
            "Serial Number",
            "CPU",
            "RAM",
            "Τύπος μνήμης",
            "Δίσκος / Storage",
            "Τύπος δίσκου",
            "GPU",
            "Λειτουργικό",
            "OPS / Mini PC",
            "Θέματα",
            "Τεχνικές σημειώσεις"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(headerRow, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(headerRow, 1, headerRow, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var currentRow = headerRow + 1;
        var counter = 1;

        foreach (var row in Rows)
        {
            worksheet.Cell(currentRow, 1).Value = counter;
            worksheet.Cell(currentRow, 2).Value = row.RoomName;
            worksheet.Cell(currentRow, 3).Value = row.CategoryName;
            worksheet.Cell(currentRow, 4).Value = row.ItemName;
            worksheet.Cell(currentRow, 5).Value = row.Brand;
            worksheet.Cell(currentRow, 6).Value = row.Model;
            worksheet.Cell(currentRow, 7).Value = row.SerialNumber;
            worksheet.Cell(currentRow, 8).Value = row.Processor;
            worksheet.Cell(currentRow, 9).Value = row.MemoryRam;
            worksheet.Cell(currentRow, 10).Value = row.MemoryType;
            worksheet.Cell(currentRow, 11).Value = row.Storage;
            worksheet.Cell(currentRow, 12).Value = row.StorageType;
            worksheet.Cell(currentRow, 13).Value = row.Graphics;
            worksheet.Cell(currentRow, 14).Value = row.OperatingSystem;
            worksheet.Cell(currentRow, 15).Value = row.OpsModuleModel;
            worksheet.Cell(currentRow, 16).Value = row.IssueText;
            worksheet.Cell(currentRow, 17).Value = row.TechnicalNotes;

            var rowRange = worksheet.Range(currentRow, 1, currentRow, headers.Length);

            if (row.HasIssues)
            {
                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
                worksheet.Cell(currentRow, 16).Style.Font.FontColor = XLColor.FromHtml("#9A3412");
                worksheet.Cell(currentRow, 16).Style.Font.Bold = true;
            }
            else
            {
                rowRange.Style.Fill.BackgroundColor = counter % 2 == 0
                    ? XLColor.FromHtml("#F8FAFC")
                    : XLColor.White;
            }

            currentRow++;
            counter++;
        }

        var dataLastRow = Math.Max(headerRow, currentRow - 1);
        var usedRange = worksheet.Range(headerRow, 1, dataLastRow, headers.Length);
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#94A3B8");
        usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CBD5E1");
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        usedRange.Style.Alignment.WrapText = true;

        worksheet.Column(1).Width = 6;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 18;
        worksheet.Column(4).Width = 28;
        worksheet.Column(5).Width = 16;
        worksheet.Column(6).Width = 20;
        worksheet.Column(7).Width = 20;
        worksheet.Column(8).Width = 26;
        worksheet.Column(9).Width = 14;
        worksheet.Column(10).Width = 15;
        worksheet.Column(11).Width = 20;
        worksheet.Column(12).Width = 15;
        worksheet.Column(13).Width = 20;
        worksheet.Column(14).Width = 18;
        worksheet.Column(15).Width = 20;
        worksheet.Column(16).Width = 38;
        worksheet.Column(17).Width = 36;

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Range(headerRow, 1, headerRow, headers.Length).SetAutoFilter();

        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.PageSetup.Margins.Top = 0.35;
        worksheet.PageSetup.Margins.Bottom = 0.35;
        worksheet.PageSetup.Margins.Left = 0.25;
        worksheet.PageSetup.Margins.Right = 0.25;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"technical-audit-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static bool IsTechnicalCandidate(string? itemName, string? categoryName)
    {
        var text = NormalizeForSearch(itemName + " " + categoryName);

        return text.Contains("Η/Υ")
            || text.Contains("ΥΠΟΛΟΓΙΣ")
            || text.Contains("PC")
            || text.Contains("LAPTOP")
            || text.Contains("NOTEBOOK")
            || text.Contains("MINI")
            || text.Contains("SERVER")
            || text.Contains("OPS")
            || text.Contains("ΔΙΑΔΡΑΣ")
            || text.Contains("INTERACTIVE");
    }

    private static bool IsInteractiveItem(string? itemName, string? categoryName)
    {
        var text = NormalizeForSearch(itemName + " " + categoryName);

        return text.Contains("ΔΙΑΔΡΑΣ")
            || text.Contains("INTERACTIVE")
            || text.Contains("OPS");
    }

    private static bool LooksLikeHdd(string? value)
    {
        var text = NormalizeForSearch(value);

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.Contains("SSD") || text.Contains("NVME") || text.Contains("M.2"))
        {
            return false;
        }

        return text.Contains("HDD")
            || text.Contains("ΣΚΛΗΡ")
            || text.Contains("5400")
            || text.Contains("7200");
    }

    private static bool LooksLikeOldOperatingSystem(string? value)
    {
        var text = NormalizeForSearch(value);

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Contains("WINDOWS XP")
            || text.Contains("WINDOWS 7")
            || text.Contains("WINDOWS 8")
            || text.Contains("VISTA")
            || text.Contains("32BIT")
            || text.Contains("32-BIT");
    }

    private static decimal? ExtractRamGb(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.ToUpperInvariant().Replace(",", ".");

        var gbMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*GB");
        if (gbMatch.Success && decimal.TryParse(gbMatch.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var gb))
        {
            return gb;
        }

        var numberOnly = Regex.Match(text, @"^\s*(\d+(?:\.\d+)?)\s*$");
        if (numberOnly.Success && decimal.TryParse(numberOnly.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var rawGb))
        {
            return rawGb;
        }

        var mbMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*MB");
        if (mbMatch.Success && decimal.TryParse(mbMatch.Groups[1].Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var mb))
        {
            return Math.Round(mb / 1024m, 2);
        }

        return null;
    }

    private static string NormalizeForSearch(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace("Ά", "Α")
            .Replace("Έ", "Ε")
            .Replace("Ή", "Η")
            .Replace("Ί", "Ι")
            .Replace("Ό", "Ο")
            .Replace("Ύ", "Υ")
            .Replace("Ώ", "Ω");
    }

    public class TechnicalAuditRow
    {
        public int InventoryItemId { get; set; }

        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        public string Processor { get; set; } = string.Empty;
        public string MemoryRam { get; set; } = string.Empty;
        public string MemoryType { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string StorageType { get; set; } = string.Empty;
        public string Graphics { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string OpsModuleModel { get; set; } = string.Empty;
        public string TechnicalNotes { get; set; } = string.Empty;

        public bool HasSpecs { get; set; }
        public decimal? RamGb { get; set; }
        public bool IsLowRam { get; set; }
        public bool IsHddOrUnknownStorage { get; set; }
        public bool HasOldOs { get; set; }
        public bool IsInteractiveWithoutOpsSpecs { get; set; }

        public List<string> Issues { get; set; } = new();

        public bool HasIssues => Issues.Count > 0;

        public string IssueText => Issues.Count == 0
            ? "Χωρίς εμφανή θέμα"
            : string.Join(", ", Issues);

        public string DisplayName
        {
            get
            {
                var pieces = new List<string> { ItemName };

                if (!string.IsNullOrWhiteSpace(Brand))
                {
                    pieces.Add(Brand);
                }

                if (!string.IsNullOrWhiteSpace(Model))
                {
                    pieces.Add(Model);
                }

                return string.Join(" · ", pieces);
            }
        }
    }
}
