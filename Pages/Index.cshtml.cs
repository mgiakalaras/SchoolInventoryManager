using System.Globalization;
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
    public int WorkingQuantity { get; set; }
    public int BrokenQuantity { get; set; }
    public int NeedsCheckQuantity { get; set; }
    public int UnknownQuantity { get; set; }
    public int ToWithdrawQuantity { get; set; }
    public int StoredQuantity { get; set; }

    public double WorkingPercent { get; set; }
    public double ProblemPercent { get; set; }
    public string ConditionPieStyle { get; set; } = string.Empty;

    public List<ConditionSummary> ConditionSummaries { get; set; } = new();
    public List<CategorySummary> CategorySummaries { get; set; } = new();
    public List<RoomSummary> RoomSummaries { get; set; } = new();
    public List<InventoryItem> RecentItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        Settings = await _db.SchoolSettings.FirstAsync();
        RoomsCount = await _db.Rooms.CountAsync();

        var activeItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive)
            .ToListAsync();

        ItemsCount = activeItems.Count;
        TotalQuantity = activeItems.Sum(x => x.Quantity);
        WorkingQuantity = activeItems.Where(x => x.Condition == EquipmentCondition.Working).Sum(x => x.Quantity);
        NeedsCheckQuantity = activeItems.Where(x => x.Condition == EquipmentCondition.NeedsCheck).Sum(x => x.Quantity);
        BrokenQuantity = activeItems.Where(x => x.Condition == EquipmentCondition.Broken).Sum(x => x.Quantity);
        ToWithdrawQuantity = activeItems.Where(x => x.Condition == EquipmentCondition.ToWithdraw).Sum(x => x.Quantity);
        StoredQuantity = activeItems.Where(x => x.Condition == EquipmentCondition.Stored).Sum(x => x.Quantity);
        UnknownQuantity = activeItems.Where(x => x.Condition == EquipmentCondition.Unknown).Sum(x => x.Quantity);

        WorkingPercent = Percent(WorkingQuantity, TotalQuantity);
        ProblemPercent = Percent(BrokenQuantity + NeedsCheckQuantity + ToWithdrawQuantity + UnknownQuantity, TotalQuantity);

        ConditionSummaries = new List<ConditionSummary>
        {
            BuildConditionSummary(EquipmentCondition.Working, WorkingQuantity, "dot-working"),
            BuildConditionSummary(EquipmentCondition.NeedsCheck, NeedsCheckQuantity, "dot-warning"),
            BuildConditionSummary(EquipmentCondition.Broken, BrokenQuantity, "dot-danger"),
            BuildConditionSummary(EquipmentCondition.ToWithdraw, ToWithdrawQuantity, "dot-dark"),
            BuildConditionSummary(EquipmentCondition.Stored, StoredQuantity, "dot-info"),
            BuildConditionSummary(EquipmentCondition.Unknown, UnknownQuantity, "dot-muted")
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
            (UnknownQuantity, "#94a3b8")
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
    }

    private ConditionSummary BuildConditionSummary(EquipmentCondition condition, int quantity, string dotClass)
    {
        return new ConditionSummary(
            condition.GetDisplayName(),
            quantity,
            Percent(quantity, TotalQuantity),
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
