using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.InventoryAudits;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IList<InventoryAuditFolder> Folders { get; set; } = new List<InventoryAuditFolder>();

    public async Task OnGetAsync()
    {
        Folders = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
            .OrderByDescending(x => x.AuditDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
