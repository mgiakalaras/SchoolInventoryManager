using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Rooms;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IList<Room> Rooms { get; set; } = new List<Room>();

    public int TotalRooms { get; set; }
    public int TotalItemRows { get; set; }
    public int TotalQuantity { get; set; }
    public int EmptyRooms { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public async Task OnGetAsync()
    {
        var query = _db.Rooms
            .Include(x => x.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var q = Q.Trim();
            query = query.Where(x =>
                x.Name.Contains(q) ||
                (x.RoomType != null && x.RoomType.Contains(q)) ||
                (x.Floor != null && x.Floor.Contains(q)) ||
                (x.Notes != null && x.Notes.Contains(q)));
        }

        Rooms = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        BuildStats();
    }

    private void BuildStats()
    {
        TotalRooms = Rooms.Count;
        TotalItemRows = Rooms.Sum(x => x.Items.Count(i => i.IsActive));
        TotalQuantity = Rooms.Sum(x => x.Items.Where(i => i.IsActive).Sum(i => i.Quantity));
        EmptyRooms = Rooms.Count(x => !x.Items.Any(i => i.IsActive));
    }
}
