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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        Item = item;
        await LoadListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
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
        existing.Description = Item.Description;
        existing.Condition = Item.Condition;
        existing.Notes = Item.Notes;
        existing.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return RedirectToPage("Index", new { roomId = existing.RoomId });
    }

    private async Task LoadListsAsync()
    {
        ViewData["Rooms"] = new SelectList(await _db.Rooms.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewData["Categories"] = new SelectList(await _db.InventoryCategories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
    }
}
