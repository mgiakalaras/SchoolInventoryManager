using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Api.Mobile;

public static class MobileAuditLiveCalculator
{
    public static async Task RecalculateFolderAsync(AppDbContext db, InventoryAuditFolder folder)
    {
        if (folder.RoomSessions == null || folder.RoomSessions.Count == 0)
        {
            return;
        }

        foreach (var session in folder.RoomSessions)
        {
            await RecalculateSessionAsync(db, session);
        }
    }

    public static async Task RecalculateSessionAsync(AppDbContext db, InventoryAuditRoomSession session)
    {
        var expectedIds = await GetExpectedItemIdsAsync(db, session.RoomId, session.RoomNameSnapshot);

        var logs = await db.InventoryAuditScanLogs
            .Where(x => x.InventoryAuditRoomSessionId == session.Id)
            .AsNoTracking()
            .ToListAsync();

        var found = logs
            .Where(x =>
                x.Status == AuditScanStatus.Found &&
                x.InventoryItemId.HasValue &&
                expectedIds.Contains(x.InventoryItemId.Value))
            .Select(x => x.InventoryItemId!.Value)
            .Distinct()
            .Count();

        var wrong = logs
            .Where(x => x.Status == AuditScanStatus.WrongRoom)
            .Select(x => x.InventoryItemId?.ToString() ?? x.ScannedCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Count();

        var unknown = logs
            .Where(x => x.Status == AuditScanStatus.Unknown)
            .Select(x => x.ScannedCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Count();

        var expected = expectedIds.Count;
        var missing = Math.Max(0, expected - found);
        var isCompleted = expected > 0 && missing == 0;

        session.ExpectedItemsCount = expected;
        session.FoundItemsCount = found;
        session.MissingItemsCount = missing;
        session.WrongRoomItemsCount = wrong;
        session.UnknownItemsCount = unknown;

        if (isCompleted)
        {
            session.CompletedAt ??= DateTime.Now;
        }
        else if (!session.IsFinalized)
        {
            session.CompletedAt = null;
        }

        session.UpdatedAt = DateTime.Now;
    }

    public static async Task<List<InventoryItem>> GetExpectedItemsAsync(AppDbContext db, InventoryAuditRoomSession session)
    {
        var roomNameSnapshot = NormalizeRoomName(session.RoomNameSnapshot);

        var items = await db.InventoryItems
            .Where(x => x.IsActive)
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .ToListAsync();

        return items
            .Where(x => IsExpectedForSession(session.RoomId, roomNameSnapshot, x))
            .OrderBy(x => x.InventoryCategory != null ? x.InventoryCategory.Name : string.Empty)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public static bool IsSameRoom(InventoryAuditRoomSession session, InventoryItem item)
    {
        var roomNameSnapshot = NormalizeRoomName(session.RoomNameSnapshot);
        return IsExpectedForSession(session.RoomId, roomNameSnapshot, item);
    }

    private static async Task<HashSet<int>> GetExpectedItemIdsAsync(AppDbContext db, int? roomId, string? roomNameSnapshotRaw)
    {
        var roomNameSnapshot = NormalizeRoomName(roomNameSnapshotRaw);

        var items = await db.InventoryItems
            .Where(x => x.IsActive)
            .Include(x => x.Room)
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.RoomId,
                RoomName = x.Room != null ? x.Room.Name : null
            })
            .ToListAsync();

        return items
            .Where(x =>
                (roomId.HasValue && x.RoomId == roomId.Value) ||
                (!string.IsNullOrWhiteSpace(roomNameSnapshot) &&
                 NormalizeRoomName(x.RoomName) == roomNameSnapshot))
            .Select(x => x.Id)
            .ToHashSet();
    }

    private static bool IsExpectedForSession(int? roomId, string roomNameSnapshot, InventoryItem item)
    {
        if (roomId.HasValue && item.RoomId == roomId.Value)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(roomNameSnapshot) &&
               NormalizeRoomName(item.Room?.Name) == roomNameSnapshot;
    }

    public static string NormalizeRoomName(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}
