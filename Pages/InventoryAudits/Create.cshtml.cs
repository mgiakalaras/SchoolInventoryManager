using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.InventoryAudits;

public class CreateModel : PageModel
{
    public const string AuditModeAnnual = "AnnualAudit";
    public const string AuditModeFirstInventory = "FirstInventory";

    private const string AuditModeAnnualMarker = "[AuditMode:AnnualAudit]";
    private const string AuditModeFirstInventoryMarker = "[AuditMode:FirstInventory]";

    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InventoryAuditFolder Folder { get; set; } = new();

    [BindProperty]
    public bool IncludeEmptyRooms { get; set; }

    [BindProperty]
    public string AuditMode { get; set; } = AuditModeAnnual;

    public int RoomsWithActiveItemsCount { get; set; }

    public int ActiveItemsCount { get; set; }

    public string SettingsSchoolName { get; set; } = "Σχολική Μονάδα";

    public string SettingsSchoolType { get; set; } = "Δεν έχει οριστεί";

    public string SettingsSchoolYear { get; set; } = string.Empty;

    public string SettingsResponsibleName { get; set; } = string.Empty;

    public string SettingsInventoryDate { get; set; } = string.Empty;

    public bool MissingSchoolSettings { get; set; }

    public async Task OnGetAsync()
    {
        await PrepareDefaultsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await ApplySchoolSettingsSnapshotAsync();

        NormalizeAuditMode();

        if (string.IsNullOrWhiteSpace(Folder.Title))
        {
            ModelState.AddModelError("Folder.Title", "Συμπλήρωσε τίτλο φακέλου.");
        }

        if (string.IsNullOrWhiteSpace(Folder.SchoolName))
        {
            ModelState.AddModelError(string.Empty, "Δεν βρέθηκε σχολική μονάδα στα Settings.");
        }

        if (!ModelState.IsValid)
        {
            await LoadSummaryAsync();
            return Page();
        }

        var now = DateTime.Now;
        var isFirstInventory = AuditMode == AuditModeFirstInventory;

        Folder.Title = Folder.Title.Trim();
        Folder.SchoolName = Folder.SchoolName.Trim();
        Folder.SchoolType = Folder.SchoolType?.Trim();
        Folder.SchoolYear = Folder.SchoolYear?.Trim();
        Folder.ResponsibleName = Folder.ResponsibleName?.Trim();
        Folder.Notes = BuildModeNotes(Folder.Notes, AuditMode);
        Folder.CreatedAt = now;
        Folder.UpdatedAt = now;

        if (!isFirstInventory)
        {
            var roomStatsQuery = _db.Rooms
                .Select(room => new
                {
                    room.Id,
                    room.Name,
                    room.SortOrder,
                    ActiveItemsCount = room.Items.Count(item => item.IsActive)
                });

            if (!IncludeEmptyRooms)
            {
                roomStatsQuery = roomStatsQuery.Where(x => x.ActiveItemsCount > 0);
            }

            var roomStats = await roomStatsQuery
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            foreach (var room in roomStats)
            {
                Folder.RoomSessions.Add(new InventoryAuditRoomSession
                {
                    RoomId = room.Id,
                    RoomNameSnapshot = room.Name,
                    ExpectedItemsCount = room.ActiveItemsCount,
                    MissingItemsCount = room.ActiveItemsCount,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        _db.InventoryAuditFolders.Add(Folder);
        await _db.SaveChangesAsync();

        TempData["Message"] = isFirstInventory
            ? "Ο φάκελος πρώτης απογραφής δημιουργήθηκε κενός. Οι χώροι και τα αντικείμενα θα προστεθούν από κινητό/tablet ή από το web app."
            : "Ο φάκελος απογραφής δημιουργήθηκε.";

        return RedirectToPage("./Details", new { id = Folder.Id });
    }

    private async Task PrepareDefaultsAsync()
    {
        await ApplySchoolSettingsSnapshotAsync();

        Folder.Title = $"Απογραφή {Folder.SchoolYear ?? DateTime.Today.Year.ToString()} - {Folder.AuditDate:dd/MM/yyyy}";

        await LoadSummaryAsync();
    }

    private async Task ApplySchoolSettingsSnapshotAsync()
    {
        var settings = await _db.SchoolSettings.AsNoTracking().FirstOrDefaultAsync();
        var today = DateTime.Today;

        SettingsSchoolName = settings?.SchoolName?.Trim() ?? "Σχολική Μονάδα";
        SettingsSchoolType = settings?.SchoolType?.Trim() ?? "Δεν έχει οριστεί";
        SettingsSchoolYear = settings?.SchoolYear?.Trim() ?? string.Empty;
        SettingsResponsibleName = settings?.InventoryManagerName?.Trim() ?? string.Empty;
        SettingsInventoryDate = (settings?.InventoryDate ?? today).ToString("dd/MM/yyyy");

        MissingSchoolSettings =
            settings == null ||
            string.IsNullOrWhiteSpace(settings.SchoolName) ||
            settings.SchoolName == "Σχολική Μονάδα";

        Folder.AuditDate = settings?.InventoryDate == default
            ? today
            : settings?.InventoryDate ?? today;

        Folder.SchoolName = SettingsSchoolName;
        Folder.SchoolType = SettingsSchoolType;
        Folder.SchoolYear = string.IsNullOrWhiteSpace(SettingsSchoolYear)
            ? today.Year.ToString()
            : SettingsSchoolYear;
        Folder.ResponsibleName = SettingsResponsibleName;
    }

    private async Task LoadSummaryAsync()
    {
        RoomsWithActiveItemsCount = await _db.Rooms
            .CountAsync(room => room.Items.Any(item => item.IsActive));

        ActiveItemsCount = await _db.InventoryItems
            .CountAsync(item => item.IsActive);
    }

    private void NormalizeAuditMode()
    {
        AuditMode = AuditMode == AuditModeFirstInventory
            ? AuditModeFirstInventory
            : AuditModeAnnual;
    }

    private static string BuildModeNotes(string? notes, string auditMode)
    {
        var marker = auditMode == AuditModeFirstInventory
            ? AuditModeFirstInventoryMarker
            : AuditModeAnnualMarker;

        var cleaned = notes?.Trim() ?? string.Empty;

        cleaned = cleaned
            .Replace(AuditModeAnnualMarker, string.Empty)
            .Replace(AuditModeFirstInventoryMarker, string.Empty)
            .Trim();

        return string.IsNullOrWhiteSpace(cleaned)
            ? marker
            : $"{marker}{Environment.NewLine}{cleaned}";
    }
}
