using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
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

        // Destruction records reference InventoryItems through DestructionBatchItems.
        // Delete dependent destruction data first, otherwise SQLite blocks the inventory cleanup with FK constraint failed.
        var deletedSparePartUsageLogs = await _db.SparePartUsageLogs.ExecuteDeleteAsync();
        var deletedCommitteeMembers = await _db.SparePartUsageLogs.ExecuteDeleteAsync();
        await _db.DestructionCommitteeMembers.ExecuteDeleteAsync();
        var deletedDestructionItems = await _db.DestructionBatchItems.ExecuteDeleteAsync();
        var deletedDestructionBatches = await _db.DestructionBatches.ExecuteDeleteAsync();

        var deletedItems = await _db.InventoryItems.ExecuteDeleteAsync();

        await TryResetSqliteSequencesAsync(
            "DestructionCommitteeMembers",
            "DestructionBatchItems",
            "DestructionBatches",
            "InventoryItems");

        CleanTemporaryImportFiles();

        SuccessMessage =
            $"Έγινε εκκαθάριση εξοπλισμού. Διαγράφηκαν {deletedItems} εγγραφές εξοπλισμού, " +
            $"{deletedDestructionBatches} φάκελοι καταστροφής, {deletedDestructionItems} γραμμές φακέλων, " +
            $"{deletedCommitteeMembers} μέλη επιτροπών και {deletedSparePartUsageLogs} κινήσεις ανταλλακτικών. " +
            "Οι χώροι, οι κατηγορίες, το απόθεμα ανταλλακτικών και τα στοιχεία σχολείου έμειναν ανέπαφα.";

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

        // Delete children/dependent records before parent tables to satisfy SQLite foreign keys.
        await _db.DestructionCommitteeMembers.ExecuteDeleteAsync();
        await _db.DestructionBatchItems.ExecuteDeleteAsync();
        await _db.DestructionBatches.ExecuteDeleteAsync();

        await _db.InventoryItems.ExecuteDeleteAsync();
        await _db.SparePartStocks.ExecuteDeleteAsync();
        await _db.Rooms.ExecuteDeleteAsync();
        await _db.InventoryCategories.ExecuteDeleteAsync();
        await _db.SchoolSettings.ExecuteDeleteAsync();

        await TryResetSqliteSequencesAsync(
            "DestructionCommitteeMembers",
            "DestructionBatchItems",
            "DestructionBatches",
            "SparePartUsageLogs",
            "InventoryItems",
            "SparePartStocks",
            "Rooms",
            "InventoryCategories",
            "SchoolSettings");

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
            SettingsCount = await _db.SchoolSettings.CountAsync(),
            DestructionBatchCount = await _db.DestructionBatches.CountAsync(),
            DestroyedItemRowCount = await _db.InventoryItems.CountAsync(x => !x.IsActive)
        };
    }

    private async Task TryResetSqliteSequencesAsync(params string[] tableNames)
    {
        if (tableNames.Length == 0)
        {
            return;
        }

        try
        {
            var sequenceParameters = tableNames
                .Select((name, index) => new SqliteParameter($"@name{index}", name))
                .ToArray();

            var sequencePlaceholders = string.Join(", ", sequenceParameters.Select(p => p.ParameterName));

            var sql = "DELETE FROM sqlite_sequence WHERE name IN (" + sequencePlaceholders + ")";

            await _db.Database.ExecuteSqlRawAsync(
                sql,
                sequenceParameters);
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
        public int DestructionBatchCount { get; set; }
        public int DestroyedItemRowCount { get; set; }
    }
}
