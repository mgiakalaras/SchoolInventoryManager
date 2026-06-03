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

    public int TotalScans { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Folder = await LoadFolderAsync(id);

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
            TempData["Warning"] = $"Υπάρχουν ακόμα {openRooms} χώροι που δεν έχουν οριστικοποιηθεί. Οριστικοποίησε πρώτα τους χώρους.";
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

    public async Task<IActionResult> OnPostClearScansAsync(int id)
    {
        var folder = await LoadFolderAsync(id);

        if (folder == null)
        {
            return NotFound();
        }

        if (folder.IsFinalized)
        {
            TempData["Warning"] = "Ο φάκελος είναι οριστικοποιημένος και δεν μπορεί να καθαριστεί.";
            return RedirectToPage(new { id });
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

        TempData["Message"] = "Οι καταχωρήσεις/scans του φακέλου καθαρίστηκαν.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var folder = await LoadFolderAsync(id);

        if (folder == null)
        {
            return NotFound();
        }

        if (folder.IsFinalized)
        {
            TempData["Warning"] = "Ο φάκελος είναι οριστικοποιημένος και δεν μπορεί να διαγραφεί.";
            return RedirectToPage(new { id });
        }

        var title = folder.Title;

        _db.InventoryAuditFolders.Remove(folder);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Ο πρόχειρος φάκελος «{title}» διαγράφηκε.";
        return RedirectToPage("/InventoryAudits/Index");
    }

    private async Task<InventoryAuditFolder?> LoadFolderAsync(int id)
    {
        return await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.Room)
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.ScanLogs)
            .FirstOrDefaultAsync(x => x.Id == id);
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
        TotalScans = Folder.RoomSessions.Sum(x => x.ScanLogs?.Count ?? 0);
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

        if (session.ExpectedItemsCount > 0 && session.MissingItemsCount == 0)
        {
            session.CompletedAt ??= DateTime.Now;
        }
        else if (!session.IsFinalized)
        {
            session.CompletedAt = null;
        }

        session.UpdatedAt = DateTime.Now;
    }
}
