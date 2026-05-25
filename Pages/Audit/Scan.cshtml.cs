using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Audit;

public class ScanModel : PageModel
{
    private readonly AppDbContext _db;

    public ScanModel(AppDbContext db)
    {
        _db = db;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetLookupAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new JsonResult(QrLookupResult.NotFound("Δεν δόθηκε κωδικός QR."));
        }

        var normalizedCode = ExtractCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new JsonResult(QrLookupResult.NotFound("Δεν αναγνωρίστηκε έγκυρος κωδικός QR."));
        }

        var item = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AssetCode == normalizedCode ||
                x.QrToken == normalizedCode);

        if (item == null)
        {
            return new JsonResult(QrLookupResult.NotFound($"Δεν βρέθηκε αντικείμενο για τον κωδικό: {normalizedCode}"));
        }

        var displayParts = new[] { item.Brand, item.Model }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var result = new QrLookupResult
        {
            Found = true,
            Code = normalizedCode,
            ItemId = item.Id,
            Name = item.Name,
            BrandModel = string.Join(" ", displayParts),
            RoomName = item.Room?.Name ?? "Χωρίς χώρο",
            CategoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
            SerialNumber = item.SerialNumber ?? string.Empty,
            ConditionText = item.Condition.GetDisplayName(),
            Quantity = item.Quantity,
            IsActive = item.IsActive,
            ItemCardUrl = Url.Page("/Items/Qr", new { code = item.AssetCode ?? item.QrToken ?? item.Id.ToString() }) ?? string.Empty,
            EditUrl = Url.Page("/Items/Edit", new { id = item.Id }) ?? string.Empty,
            Message = item.IsActive
                ? "Το αντικείμενο βρέθηκε στην ενεργή απογραφή."
                : "Το αντικείμενο βρέθηκε, αλλά δεν είναι ενεργό στην απογραφή."
        };

        return new JsonResult(result);
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

    public class QrLookupResult
    {
        public bool Found { get; set; }
        public string Code { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BrandModel { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string ConditionText { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public string ItemCardUrl { get; set; } = string.Empty;
        public string EditUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public static QrLookupResult NotFound(string message)
        {
            return new QrLookupResult
            {
                Found = false,
                Message = message
            };
        }
    }
}
