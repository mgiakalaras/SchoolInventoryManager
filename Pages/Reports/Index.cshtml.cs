using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public SchoolSettings Settings { get; set; } = new();
    public int RoomsCount { get; set; }
    public int TotalQuantity { get; set; }
    public int ProblematicQuantity { get; set; }

    public async Task OnGetAsync()
    {
        Settings = await _db.SchoolSettings.FirstAsync();
        RoomsCount = await _db.Rooms.CountAsync();
        TotalQuantity = await _db.InventoryItems.Where(x => x.IsActive).SumAsync(x => (int?)x.Quantity) ?? 0;
        ProblematicQuantity = await _db.InventoryItems
            .Where(x => x.IsActive && (x.Condition == EquipmentCondition.Broken || x.Condition == EquipmentCondition.ToWithdraw || x.Condition == EquipmentCondition.NeedsCheck))
            .SumAsync(x => (int?)x.Quantity) ?? 0;
    }
}
