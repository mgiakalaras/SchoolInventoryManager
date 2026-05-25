using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

namespace SchoolInventoryManager.Pages;

public class QModel : PageModel
{
    private readonly AppDbContext _db;

    public QModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var normalizedCode = code.Trim();

        var exists = await _db.InventoryItems
            .AsNoTracking()
            .AnyAsync(x => x.AssetCode == normalizedCode || x.QrToken == normalizedCode);

        if (!exists)
        {
            return NotFound();
        }

        return RedirectToPage("/Items/Qr", new { code = normalizedCode });
    }
}
