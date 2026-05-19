using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [BindProperty]
    public SchoolSettings Settings { get; set; } = new();

    [BindProperty]
    public IFormFile? LogoFile { get; set; }

    public async Task OnGetAsync()
    {
        Settings = await _db.SchoolSettings.FirstAsync();
        if (Settings.InventoryManagerTitle == "Εκπαιδευτικός ΠΕ86")
        {
            Settings.InventoryManagerTitle = "Υπεύθυνος/η απογραφής";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await _db.SchoolSettings.FirstAsync();
        existing.SchoolName = Settings.SchoolName;
        existing.SchoolType = Settings.SchoolType;
        existing.Address = Settings.Address;
        existing.SchoolYear = Settings.SchoolYear;
        existing.InventoryDate = Settings.InventoryDate;
        existing.InventoryManagerName = Settings.InventoryManagerName;
        existing.InventoryManagerTitle = Settings.InventoryManagerTitle == "Εκπαιδευτικός ΠΕ86"
            ? "Υπεύθυνος/η απογραφής"
            : Settings.InventoryManagerTitle;
        existing.PrincipalName = Settings.PrincipalName;
        existing.GeneralNotes = Settings.GeneralNotes;

        if (LogoFile is { Length: > 0 })
        {
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            var ext = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError(nameof(LogoFile), "Επίτρεψε μόνο εικόνες PNG/JPG/WEBP.");
                return Page();
            }

            var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadRoot);
            var filename = $"school-logo{ext}";
            var path = Path.Combine(uploadRoot, filename);
            await using var stream = System.IO.File.Create(path);
            await LogoFile.CopyToAsync(stream);
            existing.LogoPath = $"/uploads/{filename}";
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = "Τα στοιχεία σχολείου αποθηκεύτηκαν.";
        return RedirectToPage();
    }
}
