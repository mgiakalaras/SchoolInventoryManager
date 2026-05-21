using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Destructions;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IList<DestructionBatch> Batches { get; set; } = new List<DestructionBatch>();

    public int DestroyedItemsCount { get; set; }

    public async Task OnGetAsync()
    {
        Batches = await _db.DestructionBatches
            .Include(x => x.Items)
            .Include(x => x.CommitteeMembers)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        DestroyedItemsCount = await _db.DestructionBatchItems
            .CountAsync(x => x.DestructionBatch != null && x.DestructionBatch.IsFinalized);
    }
}
