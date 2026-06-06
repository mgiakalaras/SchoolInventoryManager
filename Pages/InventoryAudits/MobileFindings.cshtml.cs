using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

namespace SchoolInventoryManager.Pages.InventoryAudits;

public class MobileFindingsModel : PageModel
{
    private const string MobileAuditMarker = "[MobileAuditNewItem]";

    private readonly AppDbContext _db;

    public MobileFindingsModel(AppDbContext db)
    {
        _db = db;
    }

    public string? Q { get; set; }

    public int? RoomId { get; set; }

    public SelectList RoomOptions { get; set; } = default!;

    public List<MobileFindingItem> Items { get; set; } = new();

    public int TotalFindings { get; set; }

    public int RoomsWithFindings { get; set; }

    public int WithoutSerial { get; set; }

    public async Task OnGetAsync(string? q, int? roomId)
    {
        Q = q;
        RoomId = roomId;

        var rooms = await _db.Rooms
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync();

        RoomOptions = new SelectList(rooms, "Id", "Name", RoomId);

        var allMobileItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.Notes != null && x.Notes.Contains(MobileAuditMarker))
            .AsNoTracking()
            .ToListAsync();

        TotalFindings = allMobileItems.Count;

        // InventoryItem.RoomId is int in this project, not int?, so do not use HasValue here.
        RoomsWithFindings = allMobileItems
            .Select(x => x.RoomId)
            .Distinct()
            .Count();

        WithoutSerial = allMobileItems.Count(x => string.IsNullOrWhiteSpace(x.SerialNumber));

        var query = allMobileItems.AsEnumerable();

        if (RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == RoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var normalized = Q.Trim().ToUpperInvariant();

            query = query.Where(x =>
                Contains(x.Name, normalized) ||
                Contains(x.AssetCode, normalized) ||
                Contains(x.QrToken, normalized) ||
                Contains(x.SerialNumber, normalized) ||
                Contains(x.Brand, normalized) ||
                Contains(x.Model, normalized) ||
                Contains(x.Notes, normalized) ||
                Contains(x.Room?.Name, normalized) ||
                Contains(x.InventoryCategory?.Name, normalized));
        }

        Items = query
            .Select(x => new MobileFindingItem
            {
                Id = x.Id,
                Code = x.AssetCode ?? x.QrToken ?? x.Id.ToString(),
                Name = x.Name,
                RoomName = x.Room?.Name ?? "Χωρίς χώρο",
                CategoryName = x.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
                BrandModel = string.Join(" ", new[] { x.Brand, x.Model }.Where(v => !string.IsNullOrWhiteSpace(v))),
                SerialNumber = x.SerialNumber ?? string.Empty,
                Quantity = x.Quantity,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .OrderBy(x => x.RoomName)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();
    }

    private static bool Contains(string? value, string normalizedSearch)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.ToUpperInvariant().Contains(normalizedSearch);
    }

    public class MobileFindingItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandModel { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
