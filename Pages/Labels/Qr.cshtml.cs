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

public class QrModel : PageModel
{
    private readonly AppDbContext _db;

    public QrModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    public int[] PageSizeOptions { get; } = new[] { 25, 50, 100 };

    public SelectList Rooms { get; set; } = default!;
    public SelectList Categories { get; set; } = default!;

    public IList<QrLabelItem> Items { get; set; } = new List<QrLabelItem>();

    public string BaseUrl { get; set; } = string.Empty;
    public string InventoryDateText { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int PageStart { get; set; }
    public int PageEnd { get; set; }

    public async Task OnGetAsync()
    {
        await LoadListsAsync();

        if (!PageSizeOptions.Contains(PageSize))
        {
            PageSize = 50;
        }

        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        var settings = await _db.SchoolSettings
            .AsNoTracking()
            .FirstAsync();

        BaseUrl = ResolveBaseUrl(settings.ApplicationBaseUrl);
        InventoryDateText = settings.InventoryDate.ToString("dd/MM/yyyy");

        var query = _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
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

        if (CategoryId.HasValue)
        {
            query = query.Where(x => x.InventoryCategoryId == CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.AssetCode != null && x.AssetCode.Contains(term)) ||
                (x.Brand != null && x.Brand.Contains(term)) ||
                (x.Model != null && x.Model.Contains(term)) ||
                (x.SerialNumber != null && x.SerialNumber.Contains(term)) ||
                (x.Room != null && x.Room.Name.Contains(term)) ||
                (x.InventoryCategory != null && x.InventoryCategory.Name.Contains(term)));
        }

        TotalRows = await query.CountAsync();

        TotalPages = TotalRows == 0
            ? 1
            : (int)Math.Ceiling((double)TotalRows / PageSize);

        if (PageNumber > TotalPages)
        {
            PageNumber = TotalPages;
        }

        PageStart = TotalRows == 0
            ? 0
            : ((PageNumber - 1) * PageSize) + 1;

        PageEnd = Math.Min(PageNumber * PageSize, TotalRows);

        var inventoryItems = await query
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.InventoryCategory!.Name)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        Items = inventoryItems
            .Select(BuildLabelItem)
            .ToList();
    }

    private async Task LoadListsAsync()
    {
        Rooms = new SelectList(
            await _db.Rooms
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync(),
            "Id",
            "Name");

        Categories = new SelectList(
            await _db.InventoryCategories
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync(),
            "Id",
            "Name");
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

        // Short URL = less dense QR = easier mobile scanning.
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

    public string? PreviousPageUrl => PageNumber > 1
        ? BuildPageUrl(PageNumber - 1)
        : null;

    public string? NextPageUrl => PageNumber < TotalPages
        ? BuildPageUrl(PageNumber + 1)
        : null;

    public string BuildPageUrl(int pageNumber)
    {
        var values = new Dictionary<string, string?>
        {
            ["Search"] = Search,
            ["RoomId"] = RoomId?.ToString(),
            ["CategoryId"] = CategoryId?.ToString(),
            ["IncludeInactive"] = IncludeInactive ? "true" : null,
            ["PageSize"] = PageSize.ToString(),
            ["PageNumber"] = pageNumber.ToString()
        };

        var query = string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return string.IsNullOrWhiteSpace(query)
            ? "./Qr"
            : $"./Qr?{query}";
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
