using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public SchoolSettings Settings { get; set; } = new();
    public int RoomsCount { get; set; }
    public int ItemsCount { get; set; }
    public int TotalQuantity { get; set; }
    public int ConditionPieTotalQuantity { get; set; }
    public int WorkingQuantity { get; set; }
    public int BrokenQuantity { get; set; }
    public int NeedsCheckQuantity { get; set; }
    public int UnknownQuantity { get; set; }
    public int ToWithdrawQuantity { get; set; }
    public int StoredQuantity { get; set; }
    public int DestroyedQuantity { get; set; }

    public double WorkingPercent { get; set; }
    public double ProblemPercent { get; set; }
    public string ConditionPieStyle { get; set; } = string.Empty;

    public List<ConditionSummary> ConditionSummaries { get; set; } = new();
    public List<CategorySummary> CategorySummaries { get; set; } = new();
    public List<RoomSummary> RoomSummaries { get; set; } = new();
    public List<InventoryItem> RecentItems { get; set; } = new();

    public int SparePartRowsCount { get; set; }
    public int SparePartTotalQuantity { get; set; }
    public int LowStockSparePartRowsCount { get; set; }
    public List<DashboardLowStockPart> LowStockParts { get; set; } = new();

    public int TechnicalDeviceCount { get; set; }
    public int TechnicalIssueCount { get; set; }
    public int MissingTechnicalSpecsCount { get; set; }
    public int LowRamTechnicalCount { get; set; }
    public int StorageUpgradeCandidateCount { get; set; }
    public List<DashboardTechnicalIssueItem> TechnicalIssueItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        Settings = await _db.SchoolSettings.FirstAsync();
        RoomsCount = await _db.Rooms.CountAsync();

        var activeItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive)
            .ToListAsync();

        // For the condition pie we include destroyed items too, so the dashboard shows
        // what percentage of the registered equipment has gone to destruction.
        var conditionItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive || x.Condition == EquipmentCondition.Destroyed)
            .ToListAsync();

        ItemsCount = activeItems.Count;
        TotalQuantity = activeItems.Sum(x => x.Quantity);

        WorkingQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.Working).Sum(x => x.Quantity);
        NeedsCheckQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.NeedsCheck).Sum(x => x.Quantity);
        BrokenQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.Broken).Sum(x => x.Quantity);
        ToWithdrawQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.ToWithdraw).Sum(x => x.Quantity);
        StoredQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.Stored).Sum(x => x.Quantity);
        UnknownQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.Unknown).Sum(x => x.Quantity);
        DestroyedQuantity = conditionItems.Where(x => x.Condition == EquipmentCondition.Destroyed).Sum(x => x.Quantity);
        ConditionPieTotalQuantity = conditionItems.Sum(x => x.Quantity);

        WorkingPercent = Percent(activeItems.Where(x => x.Condition == EquipmentCondition.Working).Sum(x => x.Quantity), TotalQuantity);
        ProblemPercent = Percent(
            activeItems.Where(x =>
                    x.Condition == EquipmentCondition.Broken ||
                    x.Condition == EquipmentCondition.NeedsCheck ||
                    x.Condition == EquipmentCondition.ToWithdraw ||
                    x.Condition == EquipmentCondition.Unknown)
                .Sum(x => x.Quantity),
            TotalQuantity);

        ConditionSummaries = new List<ConditionSummary>
        {
            BuildConditionSummary(EquipmentCondition.Working, WorkingQuantity, "dot-working"),
            BuildConditionSummary(EquipmentCondition.NeedsCheck, NeedsCheckQuantity, "dot-warning"),
            BuildConditionSummary(EquipmentCondition.Broken, BrokenQuantity, "dot-danger"),
            BuildConditionSummary(EquipmentCondition.ToWithdraw, ToWithdrawQuantity, "dot-dark"),
            BuildConditionSummary(EquipmentCondition.Stored, StoredQuantity, "dot-info"),
            BuildConditionSummary(EquipmentCondition.Unknown, UnknownQuantity, "dot-muted"),
            BuildConditionSummary(EquipmentCondition.Destroyed, DestroyedQuantity, "dot-destroyed")
        }
        .Where(x => x.Quantity > 0)
        .OrderByDescending(x => x.Quantity)
        .ToList();

        ConditionPieStyle = BuildConicGradient(
            (WorkingQuantity, "var(--success)"),
            (NeedsCheckQuantity, "var(--warning)"),
            (BrokenQuantity, "var(--danger)"),
            (ToWithdrawQuantity, "#64748b"),
            (StoredQuantity, "var(--info)"),
            (UnknownQuantity, "#94a3b8"),
            (DestroyedQuantity, "#f97316")
        );

        CategorySummaries = activeItems
            .GroupBy(x => x.InventoryCategory?.Name ?? "Χωρίς κατηγορία")
            .Select(g => new CategorySummary(g.Key, g.Sum(x => x.Quantity), Percent(g.Sum(x => x.Quantity), TotalQuantity)))
            .OrderByDescending(x => x.Quantity)
            .Take(8)
            .ToList();

        RoomSummaries = activeItems
            .GroupBy(x => x.Room?.Name ?? "Χωρίς χώρο")
            .Select(g => new RoomSummary(g.Key, g.Sum(x => x.Quantity), Percent(g.Sum(x => x.Quantity), TotalQuantity)))
            .OrderByDescending(x => x.Quantity)
            .Take(8)
            .ToList();

        RecentItems = activeItems
            .OrderByDescending(x => x.UpdatedAt)
            .Take(8)
            .ToList();

        var activeSpareParts = await _db.SparePartStocks
            .Where(x => x.IsActive)
            .OrderBy(x => x.PartType)
            .ThenBy(x => x.Name)
            .ToListAsync();

        SparePartRowsCount = activeSpareParts.Count;
        SparePartTotalQuantity = activeSpareParts.Sum(x => x.Quantity);
        LowStockSparePartRowsCount = activeSpareParts.Count(x => x.IsLowStock);

        LowStockParts = activeSpareParts
            .Where(x => x.IsLowStock)
            .OrderBy(x => x.Quantity)
            .ThenBy(x => x.Name)
            .Take(5)
            .Select(x => new DashboardLowStockPart(
                x.Id,
                SparePartStock.GetPartTypeLabel(x.PartType),
                x.Name,
                x.Quantity,
                x.MinimumStock))
            .ToList();

        var technicalItems = await _db.InventoryItems
            .Where(x => x.IsActive)
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Include(x => x.TechnicalSpecs)
            .ToListAsync();

        var technicalDashboardRows = technicalItems
            .Where(x => IsTechnicalCandidate(x.Name, x.InventoryCategory?.Name))
            .Select(BuildTechnicalDashboardIssue)
            .ToList();

        TechnicalDeviceCount = technicalDashboardRows.Count;
        TechnicalIssueCount = technicalDashboardRows.Count(x => x.HasIssues);
        MissingTechnicalSpecsCount = technicalDashboardRows.Count(x => x.IsMissingSpecs);
        LowRamTechnicalCount = technicalDashboardRows.Count(x => x.IsLowRam);
        StorageUpgradeCandidateCount = technicalDashboardRows.Count(x => x.IsStorageUpgradeCandidate);

        TechnicalIssueItems = technicalDashboardRows
            .Where(x => x.HasIssues)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.RoomName)
            .ThenBy(x => x.DisplayName)
            .Take(5)
            .ToList();
    }


    private static DashboardTechnicalIssueItem BuildTechnicalDashboardIssue(InventoryItem item)
    {
        var specs = item.TechnicalSpecs;
        var hasSpecs = specs != null && specs.HasAnyValue();
        var ramGb = ExtractRamGb(specs?.MemoryRam);
        var isLowRam = ramGb.HasValue && ramGb.Value < 8;

        var storageText = string.Join(" ",
            specs?.Storage ?? string.Empty,
            specs?.StorageType ?? string.Empty).Trim();

        var isUnknownStorage = string.IsNullOrWhiteSpace(storageText);
        var isHdd = LooksLikeHdd(storageText);
        var isStorageUpgradeCandidate = isUnknownStorage || isHdd;
        var isOldOs = LooksLikeOldOperatingSystem(specs?.OperatingSystem);
        var isInteractive = IsInteractiveItem(item.Name, item.InventoryCategory?.Name);
        var isInteractiveWithoutOps = isInteractive && string.IsNullOrWhiteSpace(specs?.OpsModuleModel) && !hasSpecs;

        var issues = new List<string>();

        if (!hasSpecs)
        {
            issues.Add("Λείπουν specs");
        }

        if (isLowRam)
        {
            issues.Add("RAM < 8GB");
        }

        if (isUnknownStorage)
        {
            issues.Add("Άγνωστος δίσκος");
        }
        else if (isHdd)
        {
            issues.Add("HDD");
        }

        if (isOldOs)
        {
            issues.Add("Παλαιό OS");
        }

        if (isInteractiveWithoutOps)
        {
            issues.Add("Χωρίς OPS specs");
        }

        var displayParts = new List<string> { item.Name };

        if (!string.IsNullOrWhiteSpace(item.Brand))
        {
            displayParts.Add(item.Brand);
        }

        if (!string.IsNullOrWhiteSpace(item.Model))
        {
            displayParts.Add(item.Model);
        }

        return new DashboardTechnicalIssueItem(
            item.Id,
            item.Room?.Name ?? "Χωρίς χώρο",
            string.Join(" · ", displayParts),
            issues,
            IsMissingSpecs: !hasSpecs,
            isLowRam,
            isStorageUpgradeCandidate,
            isOldOs);
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
        if (gbMatch.Success &&
            decimal.TryParse(gbMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var gb))
        {
            return gb;
        }

        var numberOnly = Regex.Match(text, @"^\s*(\d+(?:\.\d+)?)\s*$");
        if (numberOnly.Success &&
            decimal.TryParse(numberOnly.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rawGb))
        {
            return rawGb;
        }

        var mbMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*MB");
        if (mbMatch.Success &&
            decimal.TryParse(mbMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var mb))
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

    private ConditionSummary BuildConditionSummary(EquipmentCondition condition, int quantity, string dotClass)
    {
        return new ConditionSummary(
            condition.GetDisplayName(),
            quantity,
            Percent(quantity, ConditionPieTotalQuantity),
            condition.CssClass(),
            dotClass);
    }

    private static double Percent(int value, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return Math.Round(value * 100d / total, 1);
    }

    private static string BuildConicGradient(params (int Quantity, string Color)[] segments)
    {
        var total = segments.Sum(x => x.Quantity);
        if (total <= 0)
        {
            return "background: conic-gradient(rgba(148, 163, 184, .30) 0deg 360deg);";
        }

        var current = 0d;
        var parts = new List<string>();

        foreach (var segment in segments.Where(x => x.Quantity > 0))
        {
            var next = current + (segment.Quantity * 360d / total);
            parts.Add($"{segment.Color} {FormatDegrees(current)}deg {FormatDegrees(next)}deg");
            current = next;
        }

        return $"background: conic-gradient({string.Join(", ", parts)});";
    }

    private static string FormatDegrees(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

public record ConditionSummary(string Name, int Quantity, double Percent, string BadgeClass, string DotClass);
public record CategorySummary(string Category, int Quantity, double Percent);
public record RoomSummary(string Room, int Quantity, double Percent);
public record DashboardLowStockPart(int Id, string TypeLabel, string Name, int Quantity, int MinimumStock);
public record DashboardTechnicalIssueItem(
    int Id,
    string RoomName,
    string DisplayName,
    List<string> Issues,
    bool IsMissingSpecs,
    bool IsLowRam,
    bool IsStorageUpgradeCandidate,
    bool HasOldOs)
{
    public bool HasIssues => Issues.Count > 0;

    public int Score =>
        (IsMissingSpecs ? 3 : 0) +
        (IsLowRam ? 2 : 0) +
        (IsStorageUpgradeCandidate ? 2 : 0) +
        (HasOldOs ? 1 : 0);
}
