using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Destructions;

public class SelectItemsModel : PageModel
{
    private readonly AppDbContext _db;

    public SelectItemsModel(AppDbContext db)
    {
        _db = db;
    }

    public IList<InventoryItem> Items { get; set; } = new List<InventoryItem>();
    public SelectList Rooms { get; set; } = default!;
    public SelectList Categories { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public EquipmentCondition? Condition { get; set; }

    [BindProperty]
    public List<int> SelectedItemIds { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadListsAsync();
        await LoadItemsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadListsAsync();

        if (SelectedItemIds.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Επίλεξε τουλάχιστον ένα υλικό για καταστροφή.");
            await LoadItemsAsync();
            return Page();
        }

        var settings = await _db.SchoolSettings.AsNoTracking().FirstOrDefaultAsync();

        var selectedItems = await _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive && SelectedItemIds.Contains(x.Id))
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.InventoryCategory!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();

        if (selectedItems.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Δεν βρέθηκαν ενεργά υλικά με βάση την επιλογή σου.");
            await LoadItemsAsync();
            return Page();
        }

        var now = DateTime.Now;
        var batch = new DestructionBatch
        {
            SchoolName = settings?.SchoolName ?? "Σχολική Μονάδα",
            Location = settings?.Address,
            RecommenderName = settings?.PrincipalName,
            RecommenderTitle = "Διευθυντής/ντρια",
            ChairpersonName = settings?.PrincipalName,
            ActDate = DateTime.Today,
            ProtocolDate = DateTime.Today,
            MeetingDayName = DateTime.Today.ToString("dddd", new System.Globalization.CultureInfo("el-GR")),
            MeetingTime = DateTime.Now.ToString("HH:mm"),
            CreatedAt = now,
            UpdatedAt = now
        };

        var sortOrder = 1;
        foreach (var item in selectedItems)
        {
            batch.Items.Add(new DestructionBatchItem
            {
                InventoryItemId = item.Id,
                ItemNameSnapshot = item.Name,
                BrandSnapshot = item.Brand,
                ModelSnapshot = item.Model,
                SerialNumberSnapshot = item.SerialNumber,
                RoomSnapshot = item.Room?.Name,
                CategorySnapshot = item.InventoryCategory?.Name,
                RegistryBookPageSnapshot = item.InventoryBookPage,
                QuantitySnapshot = item.Quantity,
                NotesSnapshot = item.Notes,
                SortOrder = sortOrder++
            });
        }

        batch.CommitteeMembers.Add(new DestructionCommitteeMember { FullName = string.Empty, Role = "Μέλος επιτροπής", SortOrder = 1 });
        batch.CommitteeMembers.Add(new DestructionCommitteeMember { FullName = string.Empty, Role = "Μέλος επιτροπής", SortOrder = 2 });
        batch.CommitteeMembers.Add(new DestructionCommitteeMember { FullName = string.Empty, Role = "Μέλος επιτροπής", SortOrder = 3 });

        _db.DestructionBatches.Add(batch);
        await _db.SaveChangesAsync();

        return RedirectToPage("Edit", new { id = batch.Id });
    }

    private async Task LoadListsAsync()
    {
        Rooms = new SelectList(await _db.Rooms.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
        Categories = new SelectList(await _db.InventoryCategories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(), "Id", "Name");
    }

    private async Task LoadItemsAsync()
    {
        var query = _db.InventoryItems
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!Condition.HasValue && string.IsNullOrWhiteSpace(Q) && !RoomId.HasValue && !CategoryId.HasValue)
        {
            query = query.Where(x => x.Condition == EquipmentCondition.Broken || x.Condition == EquipmentCondition.ToWithdraw || x.Condition == EquipmentCondition.NeedsCheck);
        }

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var q = Q.Trim();
            query = query.Where(x =>
                x.Name.Contains(q) ||
                (x.Brand != null && x.Brand.Contains(q)) ||
                (x.Model != null && x.Model.Contains(q)) ||
                (x.SerialNumber != null && x.SerialNumber.Contains(q)) ||
                (x.InventoryBookPage != null && x.InventoryBookPage.Contains(q)) ||
                (x.Notes != null && x.Notes.Contains(q)));
        }

        if (RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == RoomId.Value);
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(x => x.InventoryCategoryId == CategoryId.Value);
        }

        if (Condition.HasValue)
        {
            query = query.Where(x => x.Condition == Condition.Value);
        }

        Items = await query
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.InventoryCategory!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }
}
