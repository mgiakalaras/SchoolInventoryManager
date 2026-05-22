using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.TechnicalReferences;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    [BindProperty]
    public TechnicalReference NewReference { get; set; } = new();

    public List<TechnicalReference> References { get; set; } = new();

    public List<string> ReferenceTypes { get; } = new()
    {
        "Processor",
        "Memory",
        "MemoryType",
        "Storage",
        "StorageType",
        "Graphics",
        "OperatingSystem",
        "PowerSupply"
    };

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(TypeFilter))
        {
            TypeFilter = "Processor";
        }

        await LoadReferencesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewReference.ReferenceType))
        {
            ModelState.AddModelError("NewReference.ReferenceType", "Επίλεξε τύπο αναφοράς.");
        }

        if (string.IsNullOrWhiteSpace(NewReference.DisplayName))
        {
            ModelState.AddModelError("NewReference.DisplayName", "Συμπλήρωσε ονομασία.");
        }

        if (!ModelState.IsValid)
        {
            TypeFilter = NewReference.ReferenceType;
            await LoadReferencesAsync();
            return Page();
        }

        var displayName = NewReference.DisplayName.Trim();
        var referenceType = NewReference.ReferenceType.Trim();

        var exists = await _db.TechnicalReferences.AnyAsync(x =>
            x.ReferenceType == referenceType &&
            x.DisplayName == displayName);

        if (exists)
        {
            TempData["TechnicalReferenceMessage"] = "Υπάρχει ήδη ίδια εγγραφή αναφοράς.";
            return RedirectToPage("./Index", new { typeFilter = referenceType });
        }

        var maxSortOrder = await _db.TechnicalReferences
            .Where(x => x.ReferenceType == referenceType)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync() ?? 0;

        NewReference.ReferenceType = referenceType;
        NewReference.DisplayName = displayName;
        NewReference.Manufacturer = NewReference.Manufacturer?.Trim();
        NewReference.Series = NewReference.Series?.Trim();
        NewReference.ModelName = NewReference.ModelName?.Trim();
        NewReference.Detail = NewReference.Detail?.Trim();
        NewReference.Notes = NewReference.Notes?.Trim();
        NewReference.SortOrder = maxSortOrder + 10;
        NewReference.IsActive = true;
        NewReference.IsBuiltIn = false;
        NewReference.CreatedAt = DateTime.Now;
        NewReference.UpdatedAt = DateTime.Now;

        _db.TechnicalReferences.Add(NewReference);
        await _db.SaveChangesAsync();

        TempData["TechnicalReferenceMessage"] = "Η τεχνική αναφορά προστέθηκε.";
        return RedirectToPage("./Index", new { typeFilter = referenceType });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string? typeFilter)
    {
        var reference = await _db.TechnicalReferences.FindAsync(id);
        if (reference == null)
        {
            return NotFound();
        }

        reference.IsActive = !reference.IsActive;
        reference.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return RedirectToPage("./Index", new { typeFilter = typeFilter ?? reference.ReferenceType });
    }

    private async Task LoadReferencesAsync()
    {
        var query = _db.TechnicalReferences.AsQueryable();

        if (!string.IsNullOrWhiteSpace(TypeFilter))
        {
            query = query.Where(x => x.ReferenceType == TypeFilter);
        }

        References = await query
            .OrderBy(x => x.ReferenceType)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync();
    }
}
