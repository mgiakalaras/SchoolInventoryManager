using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Items;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InventoryItem Item { get; set; } = new();

    [BindProperty]
    public InventoryItemTechnicalSpecs Specs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string BackUrl => GetSafeReturnUrl() ?? Url.Page("Index") ?? "/Items";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _db.InventoryItems
            .Include(x => x.TechnicalSpecs)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        Item = item;
        Specs = item.TechnicalSpecs ?? new InventoryItemTechnicalSpecs
        {
            InventoryItemId = item.Id
        };

        await LoadListsAsync();

        ViewData["TechnicalSpecs"] = Specs;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            ViewData["TechnicalSpecs"] = Specs;
            return Page();
        }

        var existing = await _db.InventoryItems.FindAsync(Item.Id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.RoomId = Item.RoomId;
        existing.InventoryCategoryId = Item.InventoryCategoryId;
        existing.Name = Item.Name;
        existing.Quantity = Item.Quantity;
        existing.Brand = Item.Brand;
        existing.Model = Item.Model;
        existing.SerialNumber = Item.SerialNumber;
        existing.InventoryBookPage = Item.InventoryBookPage;
        existing.Description = Item.Description;
        existing.Condition = Item.Condition;
        existing.Notes = Item.Notes;
        existing.UpdatedAt = DateTime.Now;

        var existingSpecs = await _db.InventoryItemTechnicalSpecs
            .FirstOrDefaultAsync(x => x.InventoryItemId == existing.Id);

        if (Specs.HasAnyValue())
        {
            if (existingSpecs == null)
            {
                existingSpecs = new InventoryItemTechnicalSpecs
                {
                    InventoryItemId = existing.Id,
                    CreatedAt = DateTime.Now
                };

                _db.InventoryItemTechnicalSpecs.Add(existingSpecs);
            }

            existingSpecs.Processor = Specs.Processor;
            existingSpecs.MemoryRam = Specs.MemoryRam;
            existingSpecs.MemoryType = Specs.MemoryType;
            existingSpecs.Storage = Specs.Storage;
            existingSpecs.StorageType = Specs.StorageType;
            existingSpecs.Graphics = Specs.Graphics;
            existingSpecs.OperatingSystem = Specs.OperatingSystem;
            existingSpecs.LicenseInfo = Specs.LicenseInfo;
            existingSpecs.NetworkInfo = Specs.NetworkInfo;
            existingSpecs.OpsModuleModel = Specs.OpsModuleModel;
            existingSpecs.TechnicalNotes = Specs.TechnicalNotes;
            existingSpecs.UpdatedAt = DateTime.Now;
        }
        else if (existingSpecs != null)
        {
            _db.InventoryItemTechnicalSpecs.Remove(existingSpecs);
        }

        await _db.SaveChangesAsync();

        var safeReturnUrl = GetSafeReturnUrl();

        if (!string.IsNullOrWhiteSpace(safeReturnUrl))
        {
            return LocalRedirect(safeReturnUrl);
        }

        return RedirectToPage("Index", new { roomId = existing.RoomId });
    }

    private string? GetSafeReturnUrl()
    {
        if (string.IsNullOrWhiteSpace(ReturnUrl))
        {
            return null;
        }

        return Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : null;
    }

    private async Task LoadListsAsync()
    {
        ViewData["Rooms"] = new SelectList(
            await _db.Rooms.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(),
            "Id",
            "Name");

        ViewData["Categories"] = new SelectList(
            await _db.InventoryCategories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(),
            "Id",
            "Name");

        ViewData["ProcessorReferences"] = await LoadReferenceOptionsAsync("Processor");
        ViewData["MemoryReferences"] = await LoadReferenceOptionsAsync("Memory");
        ViewData["MemoryTypeReferences"] = await LoadReferenceOptionsAsync("MemoryType");
        ViewData["StorageReferences"] = await LoadReferenceOptionsAsync("Storage");
        ViewData["StorageTypeReferences"] = await LoadReferenceOptionsAsync("StorageType");
        ViewData["GraphicsReferences"] = await LoadReferenceOptionsAsync("Graphics");
        ViewData["OperatingSystemReferences"] = await LoadReferenceOptionsAsync("OperatingSystem");
    }

    private async Task<List<string>> LoadReferenceOptionsAsync(string referenceType)
    {
        return await _db.TechnicalReferences
            .Where(x => x.ReferenceType == referenceType && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => x.DisplayName)
            .ToListAsync();
    }
}
