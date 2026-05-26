using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class AuditFoldersModel : PageModel
{
    private readonly AppDbContext _db;

    public AuditFoldersModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(bool includeFinalized = false, int take = 50)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
            .AsNoTracking()
            .AsQueryable();

        if (!includeFinalized)
        {
            query = query.Where(x => !x.IsFinalized);
        }

        var folders = await query
            .OrderByDescending(x => x.AuditDate)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.AuditDate,
                x.SchoolName,
                x.SchoolType,
                x.SchoolYear,
                x.ResponsibleName,
                x.IsFinalized,
                x.FinalizedAt,
                roomSessions = x.RoomSessions.Count,
                expected = x.RoomSessions.Sum(r => r.ExpectedItemsCount),
                found = x.RoomSessions.Sum(r => r.FoundItemsCount),
                missing = x.RoomSessions.Sum(r => r.MissingItemsCount),
                wrongRoom = x.RoomSessions.Sum(r => r.WrongRoomItemsCount),
                unknown = x.RoomSessions.Sum(r => r.UnknownItemsCount),
                finalizedRooms = x.RoomSessions.Count(r => r.IsFinalized)
            })
            .ToListAsync();

        return new JsonResult(new
        {
            ok = true,
            folders
        });
    }
}
