using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Api.Mobile;

[IgnoreAntiforgeryToken]
public class HealthModel : PageModel
{
    public IActionResult OnGet()
    {
        return new JsonResult(new
        {
            ok = true,
            app = "School Inventory Manager",
            api = "mobile-scanner",
            apiVersion = "1.0",
            appVersion = AppVersion.Current,
            serverTime = DateTime.Now
        });
    }
}
