using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.SpareParts;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public SparePartStock Part { get; set; } = new();

    public IReadOnlyList<string> PartTypes => SparePartStock.PartTypes;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var part = await _db.SparePartStocks.FirstOrDefaultAsync(x => x.Id == id);

        if (part == null)
        {
            return NotFound();
        }

        Part = part;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Part.PartType))
        {
            ModelState.AddModelError("Part.PartType", "Συμπλήρωσε τύπο.");
        }

        if (string.IsNullOrWhiteSpace(Part.Name))
        {
            ModelState.AddModelError("Part.Name", "Συμπλήρωσε ονομασία.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await _db.SparePartStocks.FirstOrDefaultAsync(x => x.Id == Part.Id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.PartType = Part.PartType.Trim();
        existing.Name = Part.Name.Trim();
        existing.Manufacturer = NormalizeOptional(Part.Manufacturer);
        existing.ModelName = NormalizeOptional(Part.ModelName);
        existing.Specification = NormalizeOptional(Part.Specification);
        existing.Quantity = Math.Max(0, Part.Quantity);
        existing.MinimumStock = Math.Max(0, Part.MinimumStock);
        existing.Condition = NormalizeOptional(Part.Condition) ?? "Διαθέσιμο";
        existing.StorageLocation = NormalizeOptional(Part.StorageLocation);
        existing.CompatibleWith = NormalizeOptional(Part.CompatibleWith);
        existing.Notes = NormalizeOptional(Part.Notes);
        existing.IsActive = Part.IsActive;
        existing.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        TempData["SparePartMessage"] = "Η εγγραφή αποθέματος ενημερώθηκε.";

        return RedirectToPage("./Index", new { typeFilter = existing.PartType, showInactive = !existing.IsActive });
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
