using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SchoolInventoryManager.Pages.Audit;

public class ScanModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Scanner/Index");
    }
}
