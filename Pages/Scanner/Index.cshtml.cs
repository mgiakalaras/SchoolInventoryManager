using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

namespace SchoolInventoryManager.Pages.Scanner;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public string SuggestedServerUrl { get; set; } = "http://SERVER-IP:5148";

    public async Task OnGetAsync()
    {
        var settings = await _db.SchoolSettings.FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(settings?.ApplicationBaseUrl))
        {
            SuggestedServerUrl = settings.ApplicationBaseUrl.Trim().TrimEnd('/');
            return;
        }

        var request = HttpContext.Request;
        SuggestedServerUrl = $"{request.Scheme}://{request.Host}".TrimEnd('/');
    }
}
