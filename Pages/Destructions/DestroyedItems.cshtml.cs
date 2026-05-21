using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Destructions;

public class DestroyedItemsModel : PageModel
{
    private readonly AppDbContext _db;

    public DestroyedItemsModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IList<DestructionBatchItem> Items { get; set; } = new List<DestructionBatchItem>();

    public int TotalDestroyedItems { get; set; }

    public async Task OnGetAsync()
    {
        var query = _db.DestructionBatchItems
            .Include(x => x.DestructionBatch)
            .Include(x => x.InventoryItem)
                .ThenInclude(x => x!.Room)
            .Include(x => x.InventoryItem)
                .ThenInclude(x => x!.InventoryCategory)
            .Where(x => x.DestructionBatch != null && x.DestructionBatch.IsFinalized)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();

            query = query.Where(x =>
                x.ItemNameSnapshot.Contains(term) ||
                (x.BrandSnapshot != null && x.BrandSnapshot.Contains(term)) ||
                (x.ModelSnapshot != null && x.ModelSnapshot.Contains(term)) ||
                (x.SerialNumberSnapshot != null && x.SerialNumberSnapshot.Contains(term)) ||
                (x.RoomSnapshot != null && x.RoomSnapshot.Contains(term)) ||
                (x.CategorySnapshot != null && x.CategorySnapshot.Contains(term)) ||
                (x.RegistryBookPageSnapshot != null && x.RegistryBookPageSnapshot.Contains(term)) ||
                (x.DestructionBatch!.ActNumber != null && x.DestructionBatch.ActNumber.Contains(term)) ||
                (x.DestructionBatch.ProtocolNumber != null && x.DestructionBatch.ProtocolNumber.Contains(term)));
        }

        TotalDestroyedItems = await query.CountAsync();

        Items = await query
            .OrderByDescending(x => x.DestructionBatch!.FinalizedAt ?? x.DestructionBatch.ProtocolDate)
            .ThenBy(x => x.DestructionBatch!.ActNumber)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.ItemNameSnapshot)
            .ToListAsync();
    }
}
