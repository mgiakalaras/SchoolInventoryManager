using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Rooms;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        Room = room;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await _db.Rooms.FindAsync(Room.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = Room.Name;
        existing.RoomType = Room.RoomType;
        existing.Floor = Room.Floor;
        existing.Notes = Room.Notes;
        // SortOrder stays internal/automatic. The user should not have to maintain Α/Α or priority manually.

        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
