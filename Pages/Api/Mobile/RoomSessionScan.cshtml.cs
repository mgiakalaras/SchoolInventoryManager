using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class RoomSessionScanModel : PageModel
{
    private readonly AppDbContext _db;

    public RoomSessionScanModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var request = await ReadRequestAsync();
        var normalizedCode = MobileApiHelpers.ExtractCode(request.Code ?? request.RawValue ?? string.Empty);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Δεν δόθηκε έγκυρος κωδικός QR."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var session = await _db.InventoryAuditRoomSessions
            .Include(x => x.InventoryAuditFolder)
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

        if (session.IsFinalized || session.InventoryAuditFolder?.IsFinalized == true)
        {
            return new JsonResult(new
            {
                ok = false,
                locked = true,
                message = "Ο χώρος ή ο φάκελος είναι οριστικοποιημένος."
            })
            {
                StatusCode = StatusCodes.Status423Locked
            };
        }

        var item = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .FirstOrDefaultAsync(x => x.AssetCode == normalizedCode || x.QrToken == normalizedCode);

        MobileScanResponse response;

        if (item == null)
        {
            await SaveUnknownScanAsync(session, normalizedCode);
            await RecalculateSessionAsync(session);

            response = new MobileScanResponse
            {
                Ok = true,
                Found = false,
                Status = AuditScanStatus.Unknown,
                Code = normalizedCode,
                Message = "Άγνωστο QR. Καταγράφηκε στα άγνωστα."
            };
        }
        else
        {
            var status = item.RoomId == session.RoomId
                ? AuditScanStatus.Found
                : AuditScanStatus.WrongRoom;

            await SaveItemScanAsync(session, item, normalizedCode, status);
            await RecalculateSessionAsync(session);

            var brandModel = string.Join(" ", new[] { item.Brand, item.Model }.Where(x => !string.IsNullOrWhiteSpace(x)));

            response = new MobileScanResponse
            {
                Ok = true,
                Found = true,
                Status = status,
                Code = normalizedCode,
                Message = status == AuditScanStatus.Found
                    ? "Το αντικείμενο βρέθηκε σωστά στον χώρο."
                    : $"Το αντικείμενο είναι δηλωμένο σε άλλο χώρο: {item.Room?.Name ?? "Χωρίς χώρο"}",
                Item = new
                {
                    item.Id,
                    code = item.AssetCode ?? item.QrToken ?? item.Id.ToString(),
                    item.Name,
                    brandModel,
                    roomId = item.RoomId,
                    roomName = item.Room?.Name ?? "Χωρίς χώρο",
                    categoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
                    item.SerialNumber,
                    item.Quantity,
                    condition = item.Condition.GetDisplayName(),
                    item.IsActive
                }
            };
        }

        response.Summary = new
        {
            session.Id,
            session.ExpectedItemsCount,
            session.FoundItemsCount,
            session.MissingItemsCount,
            session.WrongRoomItemsCount,
            session.UnknownItemsCount
        };

        return new JsonResult(response);
    }

    private async Task<MobileScanRequest> ReadRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            return new MobileScanRequest
            {
                Code = Request.Form["code"].FirstOrDefault(),
                RawValue = Request.Form["rawValue"].FirstOrDefault()
            };
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<MobileScanRequest>(
                Request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return request ?? new MobileScanRequest();
        }
        catch
        {
            return new MobileScanRequest();
        }
    }

    private async Task SaveItemScanAsync(InventoryAuditRoomSession session, InventoryItem item, string code, string status)
    {
        var existing = await _db.InventoryAuditScanLogs
            .FirstOrDefaultAsync(x =>
                x.InventoryAuditRoomSessionId == session.Id &&
                x.InventoryItemId == item.Id &&
                x.Status == status);

        if (existing == null)
        {
            existing = new InventoryAuditScanLog
            {
                InventoryAuditRoomSessionId = session.Id,
                InventoryItemId = item.Id,
                ScannedCode = code,
                Status = status
            };

            _db.InventoryAuditScanLogs.Add(existing);
        }

        existing.ItemNameSnapshot = item.Name;
        existing.ExpectedRoomSnapshot = session.RoomNameSnapshot;
        existing.ActualRoomSnapshot = item.Room?.Name ?? "Χωρίς χώρο";
        existing.CategorySnapshot = item.InventoryCategory?.Name;
        existing.SerialNumberSnapshot = item.SerialNumber;
        existing.ScannedAt = DateTime.Now;

        if (!session.StartedAt.HasValue)
        {
            session.StartedAt = DateTime.Now;
        }

        session.UpdatedAt = DateTime.Now;
    }

    private async Task SaveUnknownScanAsync(InventoryAuditRoomSession session, string code)
    {
        var existing = await _db.InventoryAuditScanLogs
            .FirstOrDefaultAsync(x =>
                x.InventoryAuditRoomSessionId == session.Id &&
                x.ScannedCode == code &&
                x.Status == AuditScanStatus.Unknown);

        if (existing == null)
        {
            existing = new InventoryAuditScanLog
            {
                InventoryAuditRoomSessionId = session.Id,
                ScannedCode = code,
                Status = AuditScanStatus.Unknown,
                ItemNameSnapshot = "Άγνωστο QR",
                ExpectedRoomSnapshot = session.RoomNameSnapshot
            };

            _db.InventoryAuditScanLogs.Add(existing);
        }

        existing.ScannedAt = DateTime.Now;

        if (!session.StartedAt.HasValue)
        {
            session.StartedAt = DateTime.Now;
        }

        session.UpdatedAt = DateTime.Now;
    }

    private async Task RecalculateSessionAsync(InventoryAuditRoomSession session)
    {
        var logs = await _db.InventoryAuditScanLogs
            .Where(x => x.InventoryAuditRoomSessionId == session.Id)
            .AsNoTracking()
            .ToListAsync();

        var found = logs
            .Where(x => x.Status == AuditScanStatus.Found && x.InventoryItemId.HasValue)
            .Select(x => x.InventoryItemId!.Value)
            .Distinct()
            .Count();

        var wrong = logs
            .Where(x => x.Status == AuditScanStatus.WrongRoom)
            .Select(x => x.InventoryItemId?.ToString() ?? x.ScannedCode)
            .Distinct()
            .Count();

        var unknown = logs
            .Where(x => x.Status == AuditScanStatus.Unknown)
            .Select(x => x.ScannedCode)
            .Distinct()
            .Count();

        session.FoundItemsCount = found;
        session.MissingItemsCount = Math.Max(0, session.ExpectedItemsCount - found);
        session.WrongRoomItemsCount = wrong;
        session.UnknownItemsCount = unknown;
        session.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public class MobileScanRequest
    {
        public string? Code { get; set; }
        public string? RawValue { get; set; }
    }

    public class MobileScanResponse
    {
        public bool Ok { get; set; }
        public bool Found { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Item { get; set; }
        public object? Summary { get; set; }
    }
}
