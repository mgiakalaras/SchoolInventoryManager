using System.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Labels;

public class MobileFindingsModel : PageModel
{
    private const string MobileAuditMarker = "[MobileAuditNewItem]";

    private readonly AppDbContext _db;

    public MobileFindingsModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }

    public SelectList Rooms { get; set; } = default!;

    public IList<QrLabelItem> Items { get; set; } = new List<QrLabelItem>();

    public string BaseUrl { get; set; } = string.Empty;

    public string InventoryDateText { get; set; } = string.Empty;

    public int TotalRows { get; set; }

    public int RoomsCount { get; set; }

    public async Task OnGetAsync()
    {
        Rooms = new SelectList(
            await _db.Rooms
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync(),
            "Id",
            "Name",
            RoomId);

        var settings = await _db.SchoolSettings
            .AsNoTracking()
            .FirstAsync();

        BaseUrl = ResolveBaseUrl(settings.ApplicationBaseUrl);
        InventoryDateText = settings.InventoryDate.ToString("dd/MM/yyyy");

        var query = _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.Notes != null && x.Notes.Contains(MobileAuditMarker))
            .AsNoTracking()
            .AsQueryable();

        if (!IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == RoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.AssetCode != null && x.AssetCode.Contains(term)) ||
                (x.QrToken != null && x.QrToken.Contains(term)) ||
                (x.Brand != null && x.Brand.Contains(term)) ||
                (x.Model != null && x.Model.Contains(term)) ||
                (x.SerialNumber != null && x.SerialNumber.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)) ||
                (x.Room != null && x.Room.Name.Contains(term)) ||
                (x.InventoryCategory != null && x.InventoryCategory.Name.Contains(term)));
        }

        var inventoryItems = await query
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Name)
            .ToListAsync();

        TotalRows = inventoryItems.Count;
        RoomsCount = inventoryItems
            .Select(x => x.RoomId)
            .Distinct()
            .Count();

        Items = inventoryItems
            .Select(BuildLabelItem)
            .ToList();
    }

    private string ResolveBaseUrl(string? configuredBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.Trim().TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
    }

    private QrLabelItem BuildLabelItem(InventoryItem item)
    {
        var code = !string.IsNullOrWhiteSpace(item.AssetCode)
            ? item.AssetCode!
            : item.QrToken ?? item.Id.ToString();

        var url = $"{BaseUrl}/q/{Uri.EscapeDataString(code)}";

        return new QrLabelItem
        {
            Id = item.Id,
            AssetCode = code,
            Name = item.Name,
            RoomName = item.Room?.Name ?? "Χωρίς χώρο",
            CategoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
            Brand = item.Brand ?? string.Empty,
            Model = item.Model ?? string.Empty,
            SerialNumber = item.SerialNumber ?? string.Empty,
            Quantity = item.Quantity,
            ConditionText = item.Condition.GetDisplayName(),
            IsActive = item.IsActive,
            InventoryDateText = InventoryDateText,
            QrUrl = url,
            QrImageDataUrl = CreateQrPngDataUrl(url)
        };
    }

    private static string CreateQrPngDataUrl(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.M);

        var pngQrCode = new PngByteQRCode(data);
        var bytes = pngQrCode.GetGraphic(
            pixelsPerModule: 10,
            darkColor: Color.Black,
            lightColor: Color.White,
            drawQuietZones: true);

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    public class QrLabelItem
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string ConditionText { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string InventoryDateText { get; set; } = string.Empty;
        public string QrUrl { get; set; } = string.Empty;
        public string QrImageDataUrl { get; set; } = string.Empty;

        public string BrandModel
        {
            get
            {
                var parts = new[] { Brand, Model }
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                return string.Join(" ", parts);
            }
        }
    }
}
