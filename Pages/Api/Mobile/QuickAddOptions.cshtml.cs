using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class QuickAddOptionsModel : PageModel
{
    private const string DefaultReviewCategoryName = "Προς έλεγχο";

    private readonly AppDbContext _db;

    public QuickAddOptionsModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var categories = await _db.InventoryCategories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .ToListAsync();

        var conditions = Enum.GetValues<EquipmentCondition>()
            .Select(x => new
            {
                value = (int)x,
                name = x.ToString(),
                label = x.GetDisplayName()
            })
            .ToList();

        return new JsonResult(new
        {
            ok = true,
            defaultCategoryName = DefaultReviewCategoryName,
            quantityDefault = 1,
            quantityLabel = "Ποσότητα (συνήθως 1)",
            categories,
            conditions,
            guidance = new
            {
                primaryFieldLabel = "Τύπος αντικειμένου",
                newTypeLabel = "Νέος τύπος αντικειμένου",
                conditionLabel = "Κατάσταση λειτουργίας",
                notesLabel = "Σημειώσεις",
                reviewFlagText = "Προς έλεγχο από web app",
                quantityHelpText = "Για κανονικό εξοπλισμό άφησέ το 1. Κάθε συσκευή πρέπει να έχει ξεχωριστό QR."
            }
        });
    }
}
