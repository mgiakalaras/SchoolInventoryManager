using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class AuditFolderRoomsModel : PageModel
{
    private readonly AppDbContext _db;

    public AuditFolderRoomsModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.ScanLogs)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (folder == null)
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Ο φάκελος απογραφής δεν βρέθηκε."
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var rooms = folder.RoomSessions
            .OrderBy(x => x.RoomNameSnapshot)
            .Select(x => new
            {
                x.Id,
                roomSessionId = x.Id,
                x.RoomId,
                roomName = x.RoomNameSnapshot,
                x.ExpectedItemsCount,
                x.FoundItemsCount,
                x.MissingItemsCount,
                x.WrongRoomItemsCount,
                x.UnknownItemsCount,
                x.IsFinalized,
                x.StartedAt,
                x.CompletedAt,
                scans = x.ScanLogs.Count
            })
            .ToList();

        return new JsonResult(new
        {
            ok = true,
            folder = new
            {
                folder.Id,
                folder.Title,
                folder.AuditDate,
                folder.SchoolName,
                folder.SchoolType,
                folder.SchoolYear,
                folder.ResponsibleName,
                folder.IsFinalized
            },
            rooms
        });
    }
}
