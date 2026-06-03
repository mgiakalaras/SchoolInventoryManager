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

        var expectedItems = await LoadExpectedItemsAsync(session);
        var foundCount = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.Found && x.InventoryItemId.HasValue)
            .Select(x => x.InventoryItemId!.Value)
            .Distinct()
            .Count();

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
                expectedItemsCount = expectedItems.Count,
                foundItemsCount = foundCount,
                missingItemsCount = Math.Max(0, expectedItems.Count - foundCount),
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

    private async Task<List<object>> LoadExpectedItemsAsync(InventoryAuditRoomSession session)
    {
        var foundIds = session.ScanLogs
            .Where(x => x.Status == AuditScanStatus.Found && x.InventoryItemId.HasValue)
            .Select(x => x.InventoryItemId!.Value)
            .ToHashSet();

        /*
         * Defensive matching for mobile audit sessions:
         * 1. Prefer RoomId when it matches.
         * 2. Fallback to RoomNameSnapshot matching current item Room.Name.
         *
         * This protects the scanner flow after database reset/re-import operations.
         */
        var roomNameSnapshot = NormalizeRoomName(session.RoomNameSnapshot);

        var rawItems = await _db.InventoryItems
            .Where(x => x.IsActive)
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .ToListAsync();

        return rawItems
            .Where(x =>
                (session.RoomId.HasValue && x.RoomId == session.RoomId.Value) ||
                (!string.IsNullOrWhiteSpace(roomNameSnapshot) &&
                 NormalizeRoomName(x.Room != null ? x.Room.Name : null) == roomNameSnapshot))
            .OrderBy(x => x.InventoryCategory != null ? x.InventoryCategory.Name : string.Empty)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                code = x.AssetCode ?? x.QrToken ?? x.Id.ToString(),
                x.Name,
                brandModel = string.Join(" ", new[] { x.Brand, x.Model }.Where(v => !string.IsNullOrWhiteSpace(v))),
                categoryName = x.InventoryCategory != null ? x.InventoryCategory.Name : "Χωρίς κατηγορία",
                roomName = x.Room != null ? x.Room.Name : "Χωρίς χώρο",
                x.SerialNumber,
                x.Quantity,
                scanned = foundIds.Contains(x.Id)
            } as object)
            .ToList();
    }

    private static string NormalizeRoomName(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}
