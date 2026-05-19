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

    public async Task<IActionResult> OnGetAsync(int? roomId)
    {
        await LoadListsAsync();
        if (roomId.HasValue)
        {
            Item.RoomId = roomId.Value;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        Item.CreatedAt = DateTime.Now;
        Item.UpdatedAt = DateTime.Now;
        Item.IsActive = true;
        _db.InventoryItems.Add(Item);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index", new { roomId = Item.RoomId });
    }

    private async Task LoadListsAsync()
    {
        ViewData["Rooms"] = new SelectList(await _db.Rooms.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewData["Categories"] = new SelectList(await _db.InventoryCategories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
    }
}
