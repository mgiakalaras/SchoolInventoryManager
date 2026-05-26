using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SchoolInventoryManager.Pages.Scanner;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool ApkExists { get; set; }
    public string ApkDownloadUrl { get; set; } = "/downloads/SchoolInventoryScanner.apk";
    public string ApkFileName { get; set; } = "SchoolInventoryScanner.apk";
    public long ApkSizeBytes { get; set; }

    public string ApkSizeText
    {
        get
        {
            if (ApkSizeBytes <= 0)
            {
                return "Δεν έχει ανέβει ακόμα";
            }

            var mb = ApkSizeBytes / 1024d / 1024d;
            return $"{mb:0.0} MB";
        }
    }

    public void OnGet()
    {
        var apkPath = Path.Combine(_environment.WebRootPath, "downloads", ApkFileName);
        var file = new FileInfo(apkPath);

        ApkExists = file.Exists;
        ApkSizeBytes = ApkExists ? file.Length : 0;
    }
}
