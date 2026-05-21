using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Destructions;

public class ActModel : PageModel
{
    private readonly AppDbContext _db;

    public ActModel(AppDbContext db)
    {
        _db = db;
    }

    public DestructionBatch? Batch { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Batch = await _db.DestructionBatches
            .Include(x => x.Items)
            .Include(x => x.CommitteeMembers)
            .FirstOrDefaultAsync(x => x.Id == id);

        return Batch == null ? NotFound() : Page();
    }
}
