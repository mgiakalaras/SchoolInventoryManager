using Microsoft.AspNetCore.Mvc.RazorPages;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Pages.Update;

public class IndexModel : PageModel
{
    public string Version => AppVersion.Current;
    public string Channel => AppVersion.Channel;
    public string RepositoryUrl => AppVersion.RepositoryUrl;
    public string ReleasesUrl => AppVersion.ReleasesUrl;
}
