using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class ItemLookupModel : PageModel
{
    private readonly AppDbContext _db;

    public ItemLookupModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(string code)
    {
        var normalizedCode = MobileApiHelpers.ExtractCode(code);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new JsonResult(new
            {
                ok = false,
                found = false,
                message = "Δεν δόθηκε έγκυρος κωδικός."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var item = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssetCode == normalizedCode || x.QrToken == normalizedCode);

        if (item == null)
        {
            return new JsonResult(new
            {
                ok = true,
                found = false,
                code = normalizedCode,
                message = "Δεν βρέθηκε αντικείμενο με αυτόν τον κωδικό."
            });
        }

        var brandModel = string.Join(" ", new[] { item.Brand, item.Model }.Where(x => !string.IsNullOrWhiteSpace(x)));

        return new JsonResult(new
        {
            ok = true,
            found = true,
            item = new
            {
                item.Id,
                code = item.AssetCode ?? item.QrToken ?? item.Id.ToString(),
                item.Name,
                brandModel,
                roomId = item.RoomId,
                roomName = item.Room?.Name ?? "Χωρίς χώρο",
                categoryId = item.InventoryCategoryId,
                categoryName = item.InventoryCategory?.Name ?? "Χωρίς κατηγορία",
                item.SerialNumber,
                item.Quantity,
                condition = item.Condition.GetDisplayName(),
                item.IsActive
            }
        });
    }
}
