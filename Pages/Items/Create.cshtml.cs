using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Items;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InventoryItem Item { get; set; } = new();

    [BindProperty]
    public InventoryItemTechnicalSpecs Specs { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? roomId)
    {
        await LoadListsAsync();

        if (roomId.HasValue)
        {
            Item.RoomId = roomId.Value;
        }

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

        Item.CreatedAt = DateTime.Now;
        Item.UpdatedAt = DateTime.Now;
        Item.IsActive = true;

        _db.InventoryItems.Add(Item);
        await _db.SaveChangesAsync();

        if (Specs.HasAnyValue())
        {
            Specs.InventoryItemId = Item.Id;
            Specs.CreatedAt = DateTime.Now;
            Specs.UpdatedAt = DateTime.Now;
            _db.InventoryItemTechnicalSpecs.Add(Specs);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("Index", new { roomId = Item.RoomId });
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
