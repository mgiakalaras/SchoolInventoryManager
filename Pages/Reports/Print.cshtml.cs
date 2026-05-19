using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Reports;

public class PrintModel : PageModel
{
    private readonly AppDbContext _db;

    public PrintModel(AppDbContext db)
    {
        _db = db;
    }

    public SchoolSettings Settings { get; set; } = new();
    public IList<Room> Rooms { get; set; } = new List<Room>();
    public List<CategoryReportRow> CategoryRows { get; set; } = new();
    public List<InventoryItem> ProblematicItems { get; set; } = new();
    public bool AutoPrint { get; set; }

    public async Task OnGetAsync(bool autoPrint = false)
    {
        AutoPrint = autoPrint;
        Settings = await _db.SchoolSettings.FirstAsync();

        Rooms = await _db.Rooms
            .Include(x => x.Items.Where(i => i.IsActive))
                .ThenInclude(x => x.InventoryCategory)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var activeItemsForCategoryRows = await _db.InventoryItems
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive)
            .ToListAsync();

        CategoryRows = activeItemsForCategoryRows
            .GroupBy(x => x.InventoryCategory?.Name ?? "Χωρίς κατηγορία")
            .Select(g => new CategoryReportRow(g.Key, g.Sum(x => x.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .ToList();

        ProblematicItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive && (x.Condition == EquipmentCondition.Broken || x.Condition == EquipmentCondition.ToWithdraw || x.Condition == EquipmentCondition.NeedsCheck))
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }
}

public record CategoryReportRow(string Category, int Quantity);
