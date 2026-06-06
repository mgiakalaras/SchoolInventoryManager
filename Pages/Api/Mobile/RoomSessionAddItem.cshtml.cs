using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class RoomSessionAddItemModel : PageModel
{
    private const string DefaultMobileCategoryName = "Προς έλεγχο";
    private const string MobileAuditMarker = "[MobileAuditNewItem]";

    private readonly AppDbContext _db;

    public RoomSessionAddItemModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var request = await ReadRequestAsync();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Συμπλήρωσε ονομασία αντικειμένου."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var session = await _db.InventoryAuditRoomSessions
            .Include(x => x.InventoryAuditFolder)
            .Include(x => x.Room)
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

        var room = await ResolveRoomAsync(session);

        if (room == null)
        {
            return new JsonResult(new
            {
                ok = false,
                message = "Δεν βρέθηκε ενεργός χώρος για αυτή την απογραφή."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var category = await ResolveCategoryAsync(request);
        var now = DateTime.Now;

        var item = new InventoryItem
        {
            RoomId = room.Id,
            InventoryCategoryId = category?.Id,
            Name = request.Name.Trim(),
            Quantity = Math.Clamp(request.Quantity ?? 1, 1, 10000),
            Brand = Clean(request.Brand, 120),
            Model = Clean(request.Model, 120),
            SerialNumber = Clean(request.SerialNumber, 180),
            Description = Clean(request.Description, 1000),
            Notes = BuildMobileNotes(request.Notes, session, now),
            Condition = ParseCondition(request.Condition, request.ConditionText),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.InventoryItems.Add(item);

        // First SaveChanges generates Id. AppDbContext then backfills AssetCode / QrToken.
        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(x => x.Room).LoadAsync();
        await _db.Entry(item).Reference(x => x.InventoryCategory).LoadAsync();

        var code = item.AssetCode ?? item.QrToken ?? item.Id.ToString();

        var scanLog = new InventoryAuditScanLog
        {
            InventoryAuditRoomSessionId = session.Id,
            InventoryItemId = item.Id,
            ScannedCode = code,
            Status = AuditScanStatus.Found,
            ItemNameSnapshot = item.Name,
            ExpectedRoomSnapshot = session.RoomNameSnapshot,
            ActualRoomSnapshot = item.Room?.Name ?? room.Name,
            CategorySnapshot = item.InventoryCategory?.Name ?? category?.Name,
            SerialNumberSnapshot = item.SerialNumber,
            Notes = $"{MobileAuditMarker} Created from Android/mobile audit quick add.",
            ScannedAt = now
        };

        _db.InventoryAuditScanLogs.Add(scanLog);

        if (!session.StartedAt.HasValue)
        {
            session.StartedAt = now;
        }

        session.UpdatedAt = now;

        await MobileAuditLiveCalculator.RecalculateSessionAsync(_db, session);
        await _db.SaveChangesAsync();

        return new JsonResult(new
        {
            ok = true,
            created = true,
            message = "Το νέο αντικείμενο προστέθηκε στον χώρο και καταγράφηκε ως βρέθηκε.",
            item = new
            {
                item.Id,
                code,
                item.Name,
                brandModel = string.Join(" ", new[] { item.Brand, item.Model }.Where(x => !string.IsNullOrWhiteSpace(x))),
                roomId = item.RoomId,
                roomName = item.Room?.Name ?? room.Name,
                categoryName = item.InventoryCategory?.Name ?? category?.Name ?? "Χωρίς κατηγορία",
                item.SerialNumber,
                item.Quantity,
                condition = item.Condition.GetDisplayName(),
                item.IsActive,
                needsReview = true,
                createdFromMobileAudit = true
            },
            summary = new
            {
                session.Id,
                session.ExpectedItemsCount,
                session.FoundItemsCount,
                session.MissingItemsCount,
                session.WrongRoomItemsCount,
                session.UnknownItemsCount,
                isCompleted = session.ExpectedItemsCount > 0 && session.MissingItemsCount == 0,
                session.CompletedAt
            }
        });
    }

    private async Task<Room?> ResolveRoomAsync(InventoryAuditRoomSession session)
    {
        if (session.RoomId.HasValue)
        {
            return await _db.Rooms.FirstOrDefaultAsync(x => x.Id == session.RoomId.Value);
        }

        var sessionRoomName = Normalize(session.RoomNameSnapshot);

        if (string.IsNullOrWhiteSpace(sessionRoomName))
        {
            return null;
        }

        var rooms = await _db.Rooms.ToListAsync();

        return rooms.FirstOrDefault(x => Normalize(x.Name) == sessionRoomName);
    }

    private async Task<InventoryCategory?> ResolveCategoryAsync(MobileQuickAddItemRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            var existingById = await _db.InventoryCategories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId.Value);

            if (existingById != null)
            {
                return existingById;
            }
        }

        var requestedName = string.IsNullOrWhiteSpace(request.CategoryName)
            ? DefaultMobileCategoryName
            : request.CategoryName.Trim();

        var normalizedRequestedName = Normalize(requestedName);
        var categories = await _db.InventoryCategories.ToListAsync();

        var existing = categories.FirstOrDefault(x => Normalize(x.Name) == normalizedRequestedName);

        if (existing != null)
        {
            return existing;
        }

        var category = new InventoryCategory
        {
            Name = requestedName,
            SortOrder = 999
        };

        _db.InventoryCategories.Add(category);
        await _db.SaveChangesAsync();

        return category;
    }

    private async Task<MobileQuickAddItemRequest> ReadRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            return new MobileQuickAddItemRequest
            {
                Name = Request.Form["name"].FirstOrDefault(),
                CategoryName = Request.Form["categoryName"].FirstOrDefault(),
                Brand = Request.Form["brand"].FirstOrDefault(),
                Model = Request.Form["model"].FirstOrDefault(),
                SerialNumber = Request.Form["serialNumber"].FirstOrDefault(),
                Description = Request.Form["description"].FirstOrDefault(),
                Notes = Request.Form["notes"].FirstOrDefault(),
                ConditionText = Request.Form["condition"].FirstOrDefault(),
                Quantity = TryParseInt(Request.Form["quantity"].FirstOrDefault()),
                CategoryId = TryParseInt(Request.Form["categoryId"].FirstOrDefault())
            };
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<MobileQuickAddItemRequest>(
                Request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return request ?? new MobileQuickAddItemRequest();
        }
        catch
        {
            return new MobileQuickAddItemRequest();
        }
    }

    private static EquipmentCondition ParseCondition(int? condition, string? conditionText)
    {
        if (condition.HasValue && Enum.IsDefined(typeof(EquipmentCondition), condition.Value))
        {
            return (EquipmentCondition)condition.Value;
        }

        if (!string.IsNullOrWhiteSpace(conditionText) &&
            Enum.TryParse<EquipmentCondition>(conditionText, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EquipmentCondition.NeedsCheck;
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private static string BuildMobileNotes(string? userNotes, InventoryAuditRoomSession session, DateTime now)
    {
        var parts = new List<string>
        {
            MobileAuditMarker,
            "Νέο αντικείμενο από mobile απογραφή.",
            $"AuditRoomSessionId: {session.Id}",
            $"AuditFolderId: {session.InventoryAuditFolderId}",
            $"Χώρος απογραφής: {session.RoomNameSnapshot}",
            $"Ημερομηνία: {now:yyyy-MM-dd HH:mm}"
        };

        if (!string.IsNullOrWhiteSpace(userNotes))
        {
            parts.Add($"Σημείωση χρήστη: {userNotes.Trim()}");
        }

        parts.Add("Προς έλεγχο από το web app.");

        return string.Join(Environment.NewLine, parts);
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    public class MobileQuickAddItemRequest
    {
        public string? Name { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public int? Quantity { get; set; }
        public int? Condition { get; set; }
        public string? ConditionText { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
    }
}
