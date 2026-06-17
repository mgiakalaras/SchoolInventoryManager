using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.InventoryAudits;

public class FirstInventoryModel : PageModel
{
    private const string FirstInventoryMarker = "[AuditMode:FirstInventory]";
    private const string MobileAuditMarker = "[MobileAuditNewItem]";

    private readonly AppDbContext _db;

    public FirstInventoryModel(AppDbContext db)
    {
        _db = db;
    }

    public int? FolderId { get; set; }

    public List<FirstInventoryFolderOption> Folders { get; set; } = new();

    public FirstInventoryFolderOption? SelectedFolder { get; set; }

    public List<FirstInventoryRoomGroup> Rooms { get; set; } = new();

    public string MarkerSearch { get; set; } = string.Empty;

    public int TotalRooms { get; set; }

    public int TotalItems { get; set; }

    public int PendingReviewItems { get; set; }

    public int WithoutSerialItems { get; set; }

    public int ReadyForQrItems { get; set; }

    public int EmptyRooms { get; set; }

    public async Task OnGetAsync(int? folderId)
    {
        FolderId = folderId;

        Folders = await _db.InventoryAuditFolders
            .Where(x => x.Notes != null && x.Notes.Contains(FirstInventoryMarker))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new FirstInventoryFolderOption
            {
                Id = x.Id,
                Title = x.Title,
                SchoolName = x.SchoolName,
                SchoolYear = x.SchoolYear,
                AuditDate = x.AuditDate,
                IsFinalized = x.IsFinalized,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        if (Folders.Count == 0)
        {
            return;
        }

        var selectedId = FolderId ?? Folders.First().Id;
        SelectedFolder = Folders.FirstOrDefault(x => x.Id == selectedId);

        if (SelectedFolder == null)
        {
            SelectedFolder = Folders.First();
            selectedId = SelectedFolder.Id;
        }

        FolderId = selectedId;
        MarkerSearch = $"AuditFolderId: {selectedId}";

        var folder = await _db.InventoryAuditFolders
            .Include(x => x.RoomSessions)
                .ThenInclude(x => x.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == selectedId);

        if (folder == null)
        {
            return;
        }

        var mobileItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x =>
                x.Notes != null &&
                x.Notes.Contains(MobileAuditMarker) &&
                x.Notes.Contains(MarkerSearch))
            .AsNoTracking()
            .ToListAsync();

        var roomGroups = folder.RoomSessions
            .OrderBy(x => x.Room?.SortOrder ?? 9999)
            .ThenBy(x => x.RoomNameSnapshot)
            .Select(session =>
            {
                var items = mobileItems
                    .Where(item => item.RoomId == session.RoomId)
                    .Select(ToItemDto)
                    .OrderBy(x => x.Name)
                    .ToList();

                return new FirstInventoryRoomGroup
                {
                    RoomSessionId = session.Id,
                    RoomId = session.RoomId,
                    RoomName = session.RoomNameSnapshot,
                    ExpectedItemsCount = session.ExpectedItemsCount,
                    FoundItemsCount = session.FoundItemsCount,
                    MissingItemsCount = session.MissingItemsCount,
                    WrongRoomItemsCount = session.WrongRoomItemsCount,
                    UnknownItemsCount = session.UnknownItemsCount,
                    IsFinalized = session.IsFinalized,
                    StartedAt = session.StartedAt,
                    CompletedAt = session.CompletedAt,
                    Items = items
                };
            })
            .ToList();

        var sessionRoomIds = roomGroups
            .Where(x => x.RoomId.HasValue)
            .Select(x => x.RoomId!.Value)
            .ToHashSet();

        var orphanGroups = mobileItems
            .Where(x => !sessionRoomIds.Contains(x.RoomId))
            .GroupBy(x => new
            {
                x.RoomId,
                RoomName = x.Room != null ? x.Room.Name : "Χωρίς χώρο"
            })
            .Select(group => new FirstInventoryRoomGroup
            {
                RoomId = group.Key.RoomId,
                RoomName = group.Key.RoomName,
                Items = group.Select(ToItemDto).OrderBy(x => x.Name).ToList()
            })
            .OrderBy(x => x.RoomName)
            .ToList();

        Rooms = roomGroups
            .Concat(orphanGroups)
            .OrderBy(x => x.RoomName)
            .ToList();

        TotalRooms = Rooms.Count;
        TotalItems = mobileItems.Count;
        PendingReviewItems = mobileItems.Count(x => x.Notes != null && x.Notes.Contains("Προς έλεγχο"));
        WithoutSerialItems = mobileItems.Count(x => string.IsNullOrWhiteSpace(x.SerialNumber));
        ReadyForQrItems = mobileItems.Count(x => x.IsActive);
        EmptyRooms = Rooms.Count(x => x.Items.Count == 0);
    }

    private static FirstInventoryItemDto ToItemDto(InventoryItem item)
    {
        return new FirstInventoryItemDto
        {
            Id = item.Id,
            Code = item.AssetCode ?? item.QrToken ?? item.Id.ToString(),
            Name = item.Name,
            RoomName = item.Room?.Name ?? "Χωρίς χώρο",
            CategoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
            BrandModel = string.Join(" ", new[] { item.Brand, item.Model }.Where(x => !string.IsNullOrWhiteSpace(x))),
            SerialNumber = item.SerialNumber ?? string.Empty,
            Quantity = item.Quantity,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public class FirstInventoryFolderOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? SchoolName { get; set; }
        public string? SchoolYear { get; set; }
        public DateTime AuditDate { get; set; }
        public bool IsFinalized { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FirstInventoryRoomGroup
    {
        public int RoomSessionId { get; set; }
        public int? RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int ExpectedItemsCount { get; set; }
        public int FoundItemsCount { get; set; }
        public int MissingItemsCount { get; set; }
        public int WrongRoomItemsCount { get; set; }
        public int UnknownItemsCount { get; set; }
        public bool IsFinalized { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<FirstInventoryItemDto> Items { get; set; } = new();
    }

    public class FirstInventoryItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandModel { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
