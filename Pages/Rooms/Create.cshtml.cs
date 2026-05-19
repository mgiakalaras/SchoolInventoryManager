using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Rooms;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Room.SortOrder = await GetNextSortOrderAsync();
        _db.Rooms.Add(Room);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task<int> GetNextSortOrderAsync()
    {
        return await _db.Rooms.AnyAsync()
            ? await _db.Rooms.MaxAsync(x => x.SortOrder) + 10
            : 10;
    }
}
