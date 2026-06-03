using Microsoft.AspNetCore.Mvc;
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
                .ThenInclude(x => x.ScanLogs)
            .OrderByDescending(x => x.AuditDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.ScanLogs)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (folder == null)
        {
            TempData["Warning"] = "Ο φάκελος απογραφής δεν βρέθηκε.";
            return RedirectToPage();
        }

        if (folder.IsFinalized)
        {
            TempData["Warning"] = "Ο φάκελος είναι οριστικοποιημένος και δεν μπορεί να διαγραφεί.";
            return RedirectToPage();
        }

        var title = folder.Title;
        _db.InventoryAuditFolders.Remove(folder);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Ο πρόχειρος φάκελος «{title}» διαγράφηκε.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearScansAsync(int id)
    {
        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.ScanLogs)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (folder == null)
        {
            TempData["Warning"] = "Ο φάκελος απογραφής δεν βρέθηκε.";
            return RedirectToPage();
        }

        if (folder.IsFinalized)
        {
            TempData["Warning"] = "Ο φάκελος είναι οριστικοποιημένος και δεν μπορεί να καθαριστεί.";
            return RedirectToPage();
        }

        var logs = folder.RoomSessions
            .SelectMany(x => x.ScanLogs)
            .ToList();

        _db.InventoryAuditScanLogs.RemoveRange(logs);

        foreach (var session in folder.RoomSessions)
        {
            session.FoundItemsCount = 0;
            session.MissingItemsCount = session.ExpectedItemsCount;
            session.WrongRoomItemsCount = 0;
            session.UnknownItemsCount = 0;
            session.StartedAt = null;
            session.CompletedAt = null;
            session.IsFinalized = false;
            session.UpdatedAt = DateTime.Now;
        }

        folder.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["Message"] = $"Καθαρίστηκαν όλες οι καταχωρήσεις του φακέλου «{folder.Title}».";
        return RedirectToPage();
    }
}
