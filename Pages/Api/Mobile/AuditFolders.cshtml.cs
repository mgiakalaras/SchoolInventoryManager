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
            .AsQueryable();

        if (!includeFinalized)
        {
            query = query.Where(x => !x.IsFinalized);
        }

        var folderEntities = await query
            .OrderByDescending(x => x.AuditDate)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync();

        foreach (var folder in folderEntities)
        {
            await MobileAuditLiveCalculator.RecalculateFolderAsync(_db, folder);
        }

        await _db.SaveChangesAsync();

        var folders = folderEntities
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
                completedRooms = x.RoomSessions.Count(r => r.ExpectedItemsCount > 0 && r.MissingItemsCount == 0),
                finalizedRooms = x.RoomSessions.Count(r => r.IsFinalized)
            })
            .ToList();

        return new JsonResult(new
        {
            ok = true,
            folders
        });
    }
}
