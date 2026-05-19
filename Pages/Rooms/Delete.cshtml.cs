using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Rooms;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Room Room { get; set; } = new();
    public int ItemsCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var room = await _db.Rooms.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (room == null)
        {
            return NotFound();
        }

        Room = room;
        ItemsCount = room.Items.Count;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room != null)
        {
            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
