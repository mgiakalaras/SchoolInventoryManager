using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.InventoryAudits;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailsModel(AppDbContext db)
    {
        _db = db;
    }

    public InventoryAuditFolder? Folder { get; set; }

    public int TotalExpected { get; set; }
    public int TotalFound { get; set; }
    public int TotalMissing { get; set; }
    public int TotalProblems { get; set; }
    public int FinalizedRooms { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.Room)
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.ScanLogs)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (Folder == null)
        {
            return NotFound();
        }

        CalculateTotals();
        return Page();
    }

    public async Task<IActionResult> OnPostFinalizeAsync(int id)
    {
        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (folder == null)
        {
            return NotFound();
        }

        if (folder.IsFinalized)
        {
            TempData["Message"] = "Ο φάκελος είναι ήδη οριστικοποιημένος.";
            return RedirectToPage(new { id });
        }

        var openRooms = folder.RoomSessions.Count(x => !x.IsFinalized);
        if (openRooms > 0)
        {
            TempData["Warning"] = $"Υπάρχουν ακόμα {openRooms} χώροι που δεν έχουν οριστικοποιηθεί. Οριστικοποίησε πρώτα τους χώρους ή επιβεβαίωσε ότι θέλεις να κλείσει ο φάκελος.";
            return RedirectToPage(new { id });
        }

        folder.IsFinalized = true;
        folder.FinalizedAt = DateTime.Now;
        folder.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Ο φάκελος απογραφής οριστικοποιήθηκε.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRecalculateAsync(int id)
    {
        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.ScanLogs)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (folder == null)
        {
            return NotFound();
        }

        foreach (var session in folder.RoomSessions)
        {
            RecalculateSession(session);
        }

        folder.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Τα σύνολα του φακέλου ενημερώθηκαν.";
        return RedirectToPage(new { id });
    }

    private void CalculateTotals()
    {
        if (Folder == null)
        {
            return;
        }

        TotalExpected = Folder.RoomSessions.Sum(x => x.ExpectedItemsCount);
        TotalFound = Folder.RoomSessions.Sum(x => x.FoundItemsCount);
        TotalMissing = Folder.RoomSessions.Sum(x => x.MissingItemsCount);
        TotalProblems = Folder.RoomSessions.Sum(x => x.WrongRoomItemsCount + x.UnknownItemsCount);
        FinalizedRooms = Folder.RoomSessions.Count(x => x.IsFinalized);
    }

    private static void RecalculateSession(InventoryAuditRoomSession session)
    {
        var found = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.Found && x.InventoryItemId.HasValue)
            .Select(x => x.InventoryItemId!.Value)
            .Distinct()
            .Count();

        var wrong = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.WrongRoom)
            .Select(x => x.InventoryItemId?.ToString() ?? x.ScannedCode)
            .Distinct()
            .Count();

        var unknown = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.Unknown)
            .Select(x => x.ScannedCode)
            .Distinct()
            .Count();

        session.FoundItemsCount = found;
        session.MissingItemsCount = Math.Max(0, session.ExpectedItemsCount - found);
        session.WrongRoomItemsCount = wrong;
        session.UnknownItemsCount = unknown;
        session.UpdatedAt = DateTime.Now;
    }
}
