using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class RoomSessionModel : PageModel
{
    private readonly AppDbContext _db;

    public RoomSessionModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var session = await _db.InventoryAuditRoomSessions
            .Include(x => x.InventoryAuditFolder)
            .Include(x => x.ScanLogs)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (session == null)
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Η απογραφή χώρου δεν βρέθηκε."
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var expectedItems = new List<object>();

        if (session.RoomId.HasValue)
        {
            var foundIds = session.ScanLogs
                .Where(x => x.Status == AuditScanStatus.Found && x.InventoryItemId.HasValue)
                .Select(x => x.InventoryItemId!.Value)
                .ToHashSet();

            /*
             * Important:
             * Keep the EF query simple and materialize first.
             * Do NOT use string.Join / LINQ-to-objects helpers inside the EF Select,
             * because SQLite/EF cannot translate them and the API returns HTTP 500.
             */
            var rawItems = await _db.InventoryItems
                .Where(x => x.IsActive && x.RoomId == session.RoomId.Value)
                .Include(x => x.InventoryCategory)
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    Code = x.AssetCode ?? x.QrToken ?? x.Id.ToString(),
                    x.Name,
                    x.Brand,
                    x.Model,
                    CategoryName = x.InventoryCategory != null ? x.InventoryCategory.Name : "Χωρίς κατηγορία",
                    x.SerialNumber,
                    x.Quantity
                })
                .ToListAsync();

            expectedItems = rawItems
                .Select(x => new
                {
                    x.Id,
                    code = x.Code,
                    x.Name,
                    brandModel = string.Join(" ", new[] { x.Brand, x.Model }.Where(v => !string.IsNullOrWhiteSpace(v))),
                    categoryName = x.CategoryName,
                    x.SerialNumber,
                    x.Quantity,
                    scanned = foundIds.Contains(x.Id)
                } as object)
                .ToList();
        }

        var wrongRoom = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.WrongRoom)
            .OrderByDescending(x => x.ScannedAt)
            .Select(x => new
            {
                x.Id,
                x.InventoryItemId,
                code = x.ScannedCode,
                name = x.ItemNameSnapshot,
                actualRoom = x.ActualRoomSnapshot,
                category = x.CategorySnapshot,
                serialNumber = x.SerialNumberSnapshot,
                x.ScannedAt
            })
            .ToList();

        var unknown = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.Unknown)
            .OrderByDescending(x => x.ScannedAt)
            .Select(x => new
            {
                x.Id,
                code = x.ScannedCode,
                x.ScannedAt
            })
            .ToList();

        return new JsonResult(new
        {
            ok = true,
            folder = new
            {
                session.InventoryAuditFolder?.Id,
                session.InventoryAuditFolder?.Title,
                session.InventoryAuditFolder?.IsFinalized
            },
            session = new
            {
                session.Id,
                session.RoomId,
                roomName = session.RoomNameSnapshot,
                session.ExpectedItemsCount,
                session.FoundItemsCount,
                session.MissingItemsCount,
                session.WrongRoomItemsCount,
                session.UnknownItemsCount,
                session.IsFinalized,
                session.StartedAt,
                session.CompletedAt
            },
            expectedItems,
            wrongRoom,
            unknown
        });
    }
}
