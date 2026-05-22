using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.SpareParts;

public class UsageHistoryModel : PageModel
{
    private readonly AppDbContext _db;

    public UsageHistoryModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int? SparePartStockId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IList<SparePartUsageLog> Logs { get; set; } = new List<SparePartUsageLog>();

    public SparePartStock? SelectedPart { get; set; }

    public int TotalQuantityUsed { get; set; }

    public async Task OnGetAsync()
    {
        if (SparePartStockId.HasValue)
        {
            SelectedPart = await _db.SparePartStocks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == SparePartStockId.Value);
        }

        var query = _db.SparePartUsageLogs
            .Include(x => x.SparePartStock)
            .Include(x => x.InventoryItem)
                .ThenInclude(x => x!.Room)
            .AsNoTracking()
            .AsQueryable();

        if (SparePartStockId.HasValue)
        {
            query = query.Where(x => x.SparePartStockId == SparePartStockId.Value);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();

            query = query.Where(x =>
                x.SparePartSnapshot.Contains(term) ||
                (x.TargetDescriptionSnapshot != null && x.TargetDescriptionSnapshot.Contains(term)) ||
                (x.UsedBy != null && x.UsedBy.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)));
        }

        Logs = await query
            .OrderByDescending(x => x.UsedAt)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync();

        TotalQuantityUsed = Logs.Sum(x => x.QuantityUsed);
    }
}
