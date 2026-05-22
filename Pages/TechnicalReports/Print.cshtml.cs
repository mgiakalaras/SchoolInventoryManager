using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.TechnicalReports;

public class PrintModel : PageModel
{
    private readonly AppDbContext _db;

    public PrintModel(AppDbContext db)
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

    public string PrintDateText { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

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

            rows.Add(new TechnicalAuditRow
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
                OperatingSystem = specs?.OperatingSystem ?? string.Empty,
                OpsModuleModel = specs?.OpsModuleModel ?? string.Empty,
                HasSpecs = hasSpecs,
                IsLowRam = lowRam,
                IsHddOrUnknownStorage = hddOrUnknownStorage,
                HasOldOs = oldOs,
                IsInteractiveWithoutOpsSpecs = interactiveWithoutOpsSpecs,
                Issues = issues
            });
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
        public string OperatingSystem { get; set; } = string.Empty;
        public string OpsModuleModel { get; set; } = string.Empty;

        public bool HasSpecs { get; set; }
        public bool IsLowRam { get; set; }
        public bool IsHddOrUnknownStorage { get; set; }
        public bool HasOldOs { get; set; }
        public bool IsInteractiveWithoutOpsSpecs { get; set; }

        public List<string> Issues { get; set; } = new();

        public bool HasIssues => Issues.Count > 0;

        public string IssueText => Issues.Count == 0
            ? "Χωρίς εμφανές θέμα"
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
