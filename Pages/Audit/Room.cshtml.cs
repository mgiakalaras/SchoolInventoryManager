using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Audit;

public class RoomModel : PageModel
{
    private readonly AppDbContext _db;

    public RoomModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int? FolderId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomSessionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    public SelectList Rooms { get; set; } = default!;

    public InventoryAuditFolder? Folder { get; set; }
    public InventoryAuditRoomSession? RoomSession { get; set; }

    public bool IsFolderMode => Folder != null && RoomSession != null;
    public bool IsLocked => Folder?.IsFinalized == true || RoomSession?.IsFinalized == true;

    public string SelectedRoomName { get; set; } = string.Empty;

    public List<RoomAuditExpectedItem> ExpectedItems { get; set; } = new();
    public List<RoomAuditSavedScan> SavedScans { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadRoomsAsync();

        if (RoomSessionId.HasValue)
        {
            RoomSession = await _db.InventoryAuditRoomSessions
                .Include(x => x.InventoryAuditFolder)
                .Include(x => x.Room)
                .Include(x => x.ScanLogs)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == RoomSessionId.Value);

            if (RoomSession == null)
            {
                return NotFound();
            }

            Folder = RoomSession.InventoryAuditFolder;
            FolderId = RoomSession.InventoryAuditFolderId;
            RoomId = RoomSession.RoomId;
            SelectedRoomName = RoomSession.RoomNameSnapshot;
        }
        else if (FolderId.HasValue && RoomId.HasValue)
        {
            RoomSession = await _db.InventoryAuditRoomSessions
                .Include(x => x.InventoryAuditFolder)
                .Include(x => x.Room)
                .Include(x => x.ScanLogs)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InventoryAuditFolderId == FolderId.Value && x.RoomId == RoomId.Value);

            if (RoomSession != null)
            {
                Folder = RoomSession.InventoryAuditFolder;
                RoomSessionId = RoomSession.Id;
                SelectedRoomName = RoomSession.RoomNameSnapshot;
            }
        }

        if (RoomId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(SelectedRoomName))
            {
                var room = await _db.Rooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == RoomId.Value);

                SelectedRoomName = room?.Name ?? "Άγνωστος χώρος";
            }

            await LoadExpectedItemsAsync(RoomId.Value);
            await LoadSavedScansAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnGetLookupAsync(string code, int? roomSessionId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new JsonResult(RoomAuditLookupResult.NotFound("Δεν δόθηκε κωδικός QR."));
        }

        var normalizedCode = ExtractCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new JsonResult(RoomAuditLookupResult.NotFound("Δεν αναγνωρίστηκε έγκυρος κωδικός QR."));
        }

        var item = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AssetCode == normalizedCode ||
                x.QrToken == normalizedCode);

        InventoryAuditRoomSession? session = null;

        if (roomSessionId.HasValue)
        {
            session = await _db.InventoryAuditRoomSessions
                .Include(x => x.InventoryAuditFolder)
                .FirstOrDefaultAsync(x => x.Id == roomSessionId.Value);
        }

        if (item == null)
        {
            var unknownResult = RoomAuditLookupResult.NotFound($"Δεν βρέθηκε αντικείμενο για τον κωδικό: {normalizedCode}");
            unknownResult.Code = normalizedCode;

            if (session != null && !session.IsFinalized && session.InventoryAuditFolder?.IsFinalized != true)
            {
                await SaveUnknownScanAsync(session, normalizedCode);
                await RecalculateSessionAsync(session);
                unknownResult.Persisted = true;
            }

            return new JsonResult(unknownResult);
        }

        var displayParts = new[] { item.Brand, item.Model }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var result = new RoomAuditLookupResult
        {
            Found = true,
            Code = normalizedCode,
            ItemId = item.Id,
            Name = item.Name,
            BrandModel = string.Join(" ", displayParts),
            RoomId = item.RoomId,
            RoomName = item.Room?.Name ?? "Χωρίς χώρο",
            CategoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
            SerialNumber = item.SerialNumber ?? string.Empty,
            ConditionText = item.Condition.GetDisplayName(),
            Quantity = item.Quantity,
            IsActive = item.IsActive,
            ItemCardUrl = Url.Page("/Items/Qr", new { code = item.AssetCode ?? item.QrToken ?? item.Id.ToString() }) ?? string.Empty,
            EditUrl = Url.Page("/Items/Edit", new { id = item.Id }) ?? string.Empty
        };

        if (session != null && !session.IsFinalized && session.InventoryAuditFolder?.IsFinalized != true)
        {
            await SaveItemScanAsync(session, item, normalizedCode);
            await RecalculateSessionAsync(session);
            result.Persisted = true;
        }
        else if (session?.IsFinalized == true || session?.InventoryAuditFolder?.IsFinalized == true)
        {
            result.Message = "Ο χώρος ή ο φάκελος είναι οριστικοποιημένος. Η σάρωση δεν αποθηκεύτηκε.";
        }

        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostFinalizeRoomAsync(int roomSessionId)
    {
        var session = await _db.InventoryAuditRoomSessions
            .Include(x => x.InventoryAuditFolder)
            .Include(x => x.ScanLogs)
            .FirstOrDefaultAsync(x => x.Id == roomSessionId);

        if (session == null)
        {
            return NotFound();
        }

        if (session.InventoryAuditFolder?.IsFinalized == true)
        {
            TempData["Warning"] = "Ο φάκελος είναι ήδη οριστικοποιημένος.";
            return RedirectToPage(new { FolderId = session.InventoryAuditFolderId, RoomSessionId = session.Id, RoomId = session.RoomId });
        }

        await RecalculateSessionAsync(session);

        session.IsFinalized = true;
        session.CompletedAt = DateTime.Now;
        session.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Η απογραφή του χώρου οριστικοποιήθηκε.";
        return RedirectToPage(new { FolderId = session.InventoryAuditFolderId, RoomSessionId = session.Id, RoomId = session.RoomId });
    }

    public async Task<IActionResult> OnPostReopenRoomAsync(int roomSessionId)
    {
        var session = await _db.InventoryAuditRoomSessions
            .Include(x => x.InventoryAuditFolder)
            .FirstOrDefaultAsync(x => x.Id == roomSessionId);

        if (session == null)
        {
            return NotFound();
        }

        if (session.InventoryAuditFolder?.IsFinalized == true)
        {
            TempData["Warning"] = "Ο φάκελος είναι οριστικοποιημένος και ο χώρος δεν μπορεί να ανοίξει.";
            return RedirectToPage(new { FolderId = session.InventoryAuditFolderId, RoomSessionId = session.Id, RoomId = session.RoomId });
        }

        session.IsFinalized = false;
        session.CompletedAt = null;
        session.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Η απογραφή του χώρου άνοιξε ξανά για διορθώσεις.";
        return RedirectToPage(new { FolderId = session.InventoryAuditFolderId, RoomSessionId = session.Id, RoomId = session.RoomId });
    }

    public async Task<IActionResult> OnPostClearRoomScansAsync(int roomSessionId)
    {
        var session = await _db.InventoryAuditRoomSessions
            .Include(x => x.InventoryAuditFolder)
            .FirstOrDefaultAsync(x => x.Id == roomSessionId);

        if (session == null)
        {
            return NotFound();
        }

        if (session.IsFinalized || session.InventoryAuditFolder?.IsFinalized == true)
        {
            TempData["Warning"] = "Δεν μπορεί να γίνει καθαρισμός σε οριστικοποιημένη απογραφή.";
            return RedirectToPage(new { FolderId = session.InventoryAuditFolderId, RoomSessionId = session.Id, RoomId = session.RoomId });
        }

        var logs = await _db.InventoryAuditScanLogs
            .Where(x => x.InventoryAuditRoomSessionId == roomSessionId)
            .ToListAsync();

        _db.InventoryAuditScanLogs.RemoveRange(logs);

        session.FoundItemsCount = 0;
        session.MissingItemsCount = session.ExpectedItemsCount;
        session.WrongRoomItemsCount = 0;
        session.UnknownItemsCount = 0;
        session.StartedAt = null;
        session.CompletedAt = null;
        session.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Οι σαρώσεις του χώρου καθαρίστηκαν.";
        return RedirectToPage(new { FolderId = session.InventoryAuditFolderId, RoomSessionId = session.Id, RoomId = session.RoomId });
    }

    private async Task LoadRoomsAsync()
    {
        Rooms = new SelectList(
            await _db.Rooms
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync(),
            "Id",
            "Name");
    }

    private async Task LoadExpectedItemsAsync(int roomId)
    {
        var items = await _db.InventoryItems
            .Where(x => x.IsActive && x.RoomId == roomId)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .OrderBy(x => x.InventoryCategory!.Name)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .ToListAsync();

        ExpectedItems = items.Select(ToExpectedItem).ToList();
    }

    private async Task LoadSavedScansAsync()
    {
        if (!RoomSessionId.HasValue)
        {
            return;
        }

        SavedScans = await _db.InventoryAuditScanLogs
            .Where(x => x.InventoryAuditRoomSessionId == RoomSessionId.Value)
            .AsNoTracking()
            .OrderBy(x => x.ScannedAt)
            .Select(x => new RoomAuditSavedScan
            {
                Status = x.Status,
                ItemId = x.InventoryItemId,
                Code = x.ScannedCode,
                Name = x.ItemNameSnapshot ?? "Άγνωστο QR",
                RoomName = x.ActualRoomSnapshot ?? string.Empty,
                CategoryName = x.CategorySnapshot ?? string.Empty,
                SerialNumber = x.SerialNumberSnapshot ?? string.Empty,
                ScannedAt = x.ScannedAt
            })
            .ToListAsync();
    }

    private RoomAuditExpectedItem ToExpectedItem(InventoryItem item)
    {
        var displayParts = new[] { item.Brand, item.Model }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var code = !string.IsNullOrWhiteSpace(item.AssetCode)
            ? item.AssetCode!
            : item.QrToken ?? item.Id.ToString();

        return new RoomAuditExpectedItem
        {
            Id = item.Id,
            AssetCode = code,
            Name = item.Name,
            BrandModel = string.Join(" ", displayParts),
            CategoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
            SerialNumber = item.SerialNumber ?? string.Empty,
            Quantity = item.Quantity,
            ItemCardUrl = Url.Page("/Items/Qr", new { code }) ?? string.Empty,
            EditUrl = Url.Page("/Items/Edit", new { id = item.Id }) ?? string.Empty
        };
    }

    private async Task SaveItemScanAsync(InventoryAuditRoomSession session, InventoryItem item, string code)
    {
        var status = item.RoomId == session.RoomId
            ? AuditScanStatus.Found
            : AuditScanStatus.WrongRoom;

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

    private static string ExtractCode(string value)
    {
        var input = value.Trim();

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length >= 2 &&
                segments[^2].Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(segments[^1]).Trim();
            }

            if (segments.Length >= 3 &&
                segments[^3].Equals("Items", StringComparison.OrdinalIgnoreCase) &&
                segments[^2].Equals("Qr", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(segments[^1]).Trim();
            }

            if (segments.Length > 0)
            {
                return Uri.UnescapeDataString(segments[^1]).Trim();
            }
        }

        if (input.Contains("/q/", StringComparison.OrdinalIgnoreCase))
        {
            return input[(input.LastIndexOf("/q/", StringComparison.OrdinalIgnoreCase) + 3)..]
                .Trim()
                .Trim('/');
        }

        if (input.Contains("/Items/Qr/", StringComparison.OrdinalIgnoreCase))
        {
            return input[(input.LastIndexOf("/Items/Qr/", StringComparison.OrdinalIgnoreCase) + 10)..]
                .Trim()
                .Trim('/');
        }

        return input;
    }

    public class RoomAuditExpectedItem
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BrandModel { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string ItemCardUrl { get; set; } = string.Empty;
        public string EditUrl { get; set; } = string.Empty;
    }

    public class RoomAuditSavedScan
    {
        public string Status { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
    }

    public class RoomAuditLookupResult
    {
        public bool Found { get; set; }
        public bool Persisted { get; set; }
        public string Code { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BrandModel { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string ConditionText { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public string ItemCardUrl { get; set; } = string.Empty;
        public string EditUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public static RoomAuditLookupResult NotFound(string message)
        {
            return new RoomAuditLookupResult
            {
                Found = false,
                Message = message
            };
        }
    }
}
