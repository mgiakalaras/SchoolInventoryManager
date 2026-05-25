using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Items;

public class QrModel : PageModel
{
    private readonly AppDbContext _db;

    public QrModel(AppDbContext db)
    {
        _db = db;
    }

    public InventoryItem Item { get; set; } = new();

    public string QrUrl { get; set; } = string.Empty;

    public string DisplayCode => !string.IsNullOrWhiteSpace(Item.AssetCode)
        ? Item.AssetCode!
        : Item.QrToken ?? Item.Id.ToString();

    public async Task<IActionResult> OnGetAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var normalizedCode = code.Trim();

        var item = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Include(x => x.TechnicalSpecs)
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AssetCode == normalizedCode ||
                x.QrToken == normalizedCode);

        if (item == null)
        {
            return NotFound();
        }

        Item = item;
        QrUrl = BuildQrUrl(item);

        return Page();
    }

    private string BuildQrUrl(InventoryItem item)
    {
        var code = !string.IsNullOrWhiteSpace(item.AssetCode)
            ? item.AssetCode
            : item.QrToken ?? item.Id.ToString();

        var baseUrl = _db.SchoolSettings
            .AsNoTracking()
            .Select(x => x.ApplicationBaseUrl)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        }

        return $"{baseUrl.TrimEnd('/')}/Items/Qr/{Uri.EscapeDataString(code)}";
    }
}
