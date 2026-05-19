using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;

namespace SchoolInventoryManager.Pages.Maintenance;

public class IndexModel : PageModel
{
    private const string RequiredConfirmationText = "ΚΑΘΑΡΙΣΜΟΣ";
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [BindProperty]
    public string? ConfirmText { get; set; }

    public MaintenanceStats Stats { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string ConfirmationWord => RequiredConfirmationText;

    public async Task OnGetAsync()
    {
        await LoadStatsAsync();
    }

    public async Task<IActionResult> OnPostClearInventoryAsync()
    {
        if (!IsConfirmed())
        {
            ErrorMessage = $"Για εκκαθάριση γράψε ακριβώς: {RequiredConfirmationText}";
            await LoadStatsAsync();
            return Page();
        }

        var deletedItems = await _db.InventoryItems.ExecuteDeleteAsync();
        await TryResetSqliteSequencesAsync("InventoryItems");
        CleanTemporaryImportFiles();

        SuccessMessage = $"Έγινε εκκαθάριση εξοπλισμού. Διαγράφηκαν {deletedItems} εγγραφές. Οι χώροι, οι κατηγορίες και τα στοιχεία σχολείου έμειναν ανέπαφα.";
        ConfirmText = string.Empty;
        await LoadStatsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetDatabaseAsync()
    {
        if (!IsConfirmed())
        {
            ErrorMessage = $"Για πλήρη επαναφορά γράψε ακριβώς: {RequiredConfirmationText}";
            await LoadStatsAsync();
            return Page();
        }

        await _db.InventoryItems.ExecuteDeleteAsync();
        await _db.Rooms.ExecuteDeleteAsync();
        await _db.InventoryCategories.ExecuteDeleteAsync();
        await _db.SchoolSettings.ExecuteDeleteAsync();
        await TryResetSqliteSequencesAsync("InventoryItems", "Rooms", "InventoryCategories", "SchoolSettings");
        CleanTemporaryImportFiles();

        DbSeeder.Seed(_db);

        SuccessMessage = "Έγινε πλήρης επαναφορά βάσης. Η εφαρμογή ξαναδημιούργησε τα βασικά στοιχεία, χώρους και κατηγορίες εκκίνησης.";
        ConfirmText = string.Empty;
        await LoadStatsAsync();
        return Page();
    }

    private bool IsConfirmed()
    {
        return string.Equals(ConfirmText?.Trim(), RequiredConfirmationText, StringComparison.Ordinal);
    }

    private async Task LoadStatsAsync()
    {
        var activeItems = await _db.InventoryItems
            .Where(x => x.IsActive)
            .Select(x => x.Quantity)
            .ToListAsync();

        Stats = new MaintenanceStats
        {
            RoomCount = await _db.Rooms.CountAsync(),
            CategoryCount = await _db.InventoryCategories.CountAsync(),
            ItemRowCount = activeItems.Count,
            TotalQuantity = activeItems.Sum(),
            SettingsCount = await _db.SchoolSettings.CountAsync()
        };
    }

    private async Task TryResetSqliteSequencesAsync(params string[] tableNames)
    {
        if (tableNames.Length == 0)
        {
            return;
        }

        var quotedNames = string.Join(", ", tableNames.Select(x => $"'{x.Replace("'", "''")}'"));
        try
        {
            await _db.Database.ExecuteSqlRawAsync($"DELETE FROM sqlite_sequence WHERE name IN ({quotedNames})");
        }
        catch
        {
            // sqlite_sequence exists only when AUTOINCREMENT has been created. If not present, there is nothing to reset.
        }
    }

    private void CleanTemporaryImportFiles()
    {
        try
        {
            var importFolder = Path.Combine(_environment.WebRootPath, "uploads", "imports");
            if (!Directory.Exists(importFolder))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(importFolder, "*.xlsx"))
            {
                System.IO.File.Delete(file);
            }
        }
        catch
        {
            // Temporary import file cleanup is helpful but not critical for database maintenance.
        }
    }

    public class MaintenanceStats
    {
        public int RoomCount { get; set; }
        public int CategoryCount { get; set; }
        public int ItemRowCount { get; set; }
        public int TotalQuantity { get; set; }
        public int SettingsCount { get; set; }
    }
}
