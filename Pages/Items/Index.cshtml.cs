using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Items;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IList<InventoryItem> Items { get; set; } = new List<InventoryItem>();
    public IList<InventoryGroup> GroupedItems { get; set; } = new List<InventoryGroup>();
    public SelectList Rooms { get; set; } = default!;
    public SelectList Categories { get; set; } = default!;

    public int TotalMatchingRows { get; set; }
    public int TotalMatchingQuantity { get; set; }
    public int WorkingQuantity { get; set; }
    public int AttentionQuantity { get; set; }
    public int UnknownQuantity { get; set; }
    public int DistinctRoomCount { get; set; }
    public int DistinctCategoryCount { get; set; }
    public double WorkingPercent { get; set; }
    public double AttentionPercent { get; set; }

    public IList<ConditionSummary> ConditionSummaries { get; set; } = new List<ConditionSummary>();
    public IList<ListSummary> TopRooms { get; set; } = new List<ListSummary>();
    public IList<ListSummary> TopCategories { get; set; } = new List<ListSummary>();

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public EquipmentCondition? Condition { get; set; }

    public async Task OnGetAsync()
    {
        Rooms = new SelectList(await _db.Rooms.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
        Categories = new SelectList(await _db.InventoryCategories.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");

        var query = _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var q = Q.Trim();
            query = query.Where(x =>
                x.Name.Contains(q) ||
                (x.Brand != null && x.Brand.Contains(q)) ||
                (x.Model != null && x.Model.Contains(q)) ||
                (x.SerialNumber != null && x.SerialNumber.Contains(q)) ||
                (x.Description != null && x.Description.Contains(q)) ||
                (x.Notes != null && x.Notes.Contains(q)));
        }

        if (RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == RoomId.Value);
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(x => x.InventoryCategoryId == CategoryId.Value);
        }

        if (Condition.HasValue)
        {
            query = query.Where(x => x.Condition == Condition.Value);
        }

        Items = await query
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.InventoryCategory!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();

        BuildStats();
        BuildGroups();
    }

    private void BuildStats()
    {
        TotalMatchingRows = Items.Count;
        TotalMatchingQuantity = Items.Sum(x => x.Quantity);
        WorkingQuantity = Items.Where(x => x.Condition == EquipmentCondition.Working).Sum(x => x.Quantity);
        AttentionQuantity = Items.Where(x => x.Condition is EquipmentCondition.NeedsCheck or EquipmentCondition.Broken or EquipmentCondition.ToWithdraw).Sum(x => x.Quantity);
        UnknownQuantity = Items.Where(x => x.Condition == EquipmentCondition.Unknown).Sum(x => x.Quantity);
        DistinctRoomCount = Items.Select(x => x.RoomId).Distinct().Count();
        DistinctCategoryCount = Items.Where(x => x.InventoryCategoryId.HasValue).Select(x => x.InventoryCategoryId!.Value).Distinct().Count();

        WorkingPercent = TotalMatchingQuantity == 0 ? 0 : Math.Round((double)WorkingQuantity * 100 / TotalMatchingQuantity, 1);
        AttentionPercent = TotalMatchingQuantity == 0 ? 0 : Math.Round((double)AttentionQuantity * 100 / TotalMatchingQuantity, 1);

        ConditionSummaries = Items
            .GroupBy(x => x.Condition)
            .Select(g => new ConditionSummary(
                g.Key,
                g.Key.GetDisplayName(),
                g.Sum(x => x.Quantity),
                TotalMatchingQuantity == 0 ? 0 : Math.Round((double)g.Sum(x => x.Quantity) * 100 / TotalMatchingQuantity, 1),
                g.Key.CssClass()))
            .OrderByDescending(x => x.Quantity)
            .ToList();

        TopRooms = Items
            .GroupBy(x => x.Room?.Name ?? "Χωρίς χώρο")
            .Select(g => new ListSummary(g.Key, g.Sum(x => x.Quantity), TotalMatchingQuantity == 0 ? 0 : Math.Round((double)g.Sum(x => x.Quantity) * 100 / TotalMatchingQuantity, 1)))
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToList();

        TopCategories = Items
            .GroupBy(x => x.InventoryCategory?.Name ?? "Χωρίς κατηγορία")
            .Select(g => new ListSummary(g.Key, g.Sum(x => x.Quantity), TotalMatchingQuantity == 0 ? 0 : Math.Round((double)g.Sum(x => x.Quantity) * 100 / TotalMatchingQuantity, 1)))
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToList();
    }

    private void BuildGroups()
    {
        GroupedItems = Items
            .GroupBy(x => new
            {
                x.RoomId,
                RoomName = x.Room?.Name ?? "Χωρίς χώρο",
                x.InventoryCategoryId,
                CategoryName = x.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
                NormalizedName = NormalizeGroupText(x.Name),
                DisplayName = x.Name.Trim(),
                x.Condition
            })
            .Select(g => new InventoryGroup(
                g.Key.RoomId,
                g.Key.RoomName,
                g.Key.InventoryCategoryId,
                g.Key.CategoryName,
                g.Key.DisplayName,
                g.Key.Condition,
                g.Sum(x => x.Quantity),
                g.Count(),
                g.OrderBy(x => x.Brand).ThenBy(x => x.Model).ThenBy(x => x.SerialNumber).ToList()))
            .OrderBy(x => x.RoomName)
            .ThenBy(x => x.CategoryName)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static string NormalizeGroupText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var formD = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToUpperInvariant(ch));
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public record ConditionSummary(EquipmentCondition Condition, string Name, int Quantity, double Percent, string CssClass);
    public record ListSummary(string Name, int Quantity, double Percent);

    public class InventoryGroup
    {
        public InventoryGroup(int? roomId, string roomName, int? categoryId, string categoryName, string name, EquipmentCondition condition, int totalQuantity, int rowCount, IList<InventoryItem> details)
        {
            RoomId = roomId;
            RoomName = roomName;
            CategoryId = categoryId;
            CategoryName = categoryName;
            Name = name;
            Condition = condition;
            TotalQuantity = totalQuantity;
            RowCount = rowCount;
            Details = details;
        }

        public int? RoomId { get; }
        public string RoomName { get; }
        public int? CategoryId { get; }
        public string CategoryName { get; }
        public string Name { get; }
        public EquipmentCondition Condition { get; }
        public int TotalQuantity { get; }
        public int RowCount { get; }
        public IList<InventoryItem> Details { get; }
        public bool HasMultipleDetails => Details.Count > 1 || Details.Any(x => x.Quantity > 1 || !string.IsNullOrWhiteSpace(x.SerialNumber) || !string.IsNullOrWhiteSpace(x.Brand) || !string.IsNullOrWhiteSpace(x.Model));
    }
}
