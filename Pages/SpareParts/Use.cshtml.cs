using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.SpareParts;

public class UseModel : PageModel
{
    private readonly AppDbContext _db;

    public UseModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public SparePartUsageInput Input { get; set; } = new();

    public SparePartStock? Part { get; set; }

    public SelectList InventoryItemOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPageAsync();

        if (Part == null)
        {
            return NotFound();
        }

        Input = new SparePartUsageInput
        {
            QuantityUsed = 1,
            UsedAt = DateTime.Now
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadPageAsync();

        if (Part == null)
        {
            return NotFound();
        }

        if (!Part.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Το ανταλλακτικό είναι αρχειοθετημένο και δεν μπορεί να χρησιμοποιηθεί.");
        }

        if (Input.QuantityUsed < 1)
        {
            ModelState.AddModelError("Input.QuantityUsed", "Η ποσότητα πρέπει να είναι τουλάχιστον 1.");
        }

        if (Input.QuantityUsed > Part.Quantity)
        {
            ModelState.AddModelError("Input.QuantityUsed", $"Η διαθέσιμη ποσότητα είναι {Part.Quantity}.");
        }

        InventoryItem? targetItem = null;

        if (Input.InventoryItemId.HasValue)
        {
            targetItem = await _db.InventoryItems
                .Include(x => x.Room)
                .Include(x => x.InventoryCategory)
                .FirstOrDefaultAsync(x => x.Id == Input.InventoryItemId.Value);

            if (targetItem == null)
            {
                ModelState.AddModelError("Input.InventoryItemId", "Η συσκευή/υλικό δεν βρέθηκε.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.Now;

        var usageLog = new SparePartUsageLog
        {
            SparePartStockId = Part.Id,
            InventoryItemId = targetItem?.Id,
            QuantityUsed = Input.QuantityUsed,
            UsedAt = Input.UsedAt,
            UsedBy = NormalizeOptional(Input.UsedBy),
            SparePartSnapshot = BuildSparePartSnapshot(Part),
            TargetDescriptionSnapshot = targetItem == null
                ? NormalizeOptional(Input.TargetDescription)
                : BuildInventoryItemSnapshot(targetItem),
            Notes = NormalizeOptional(Input.Notes),
            CreatedAt = now
        };

        Part.Quantity -= Input.QuantityUsed;
        Part.UpdatedAt = now;

        _db.SparePartUsageLogs.Add(usageLog);
        await _db.SaveChangesAsync();

        TempData["SparePartMessage"] = "Η χρήση ανταλλακτικού καταχωρίστηκε και το απόθεμα ενημερώθηκε.";

        return RedirectToPage("./UsageHistory", new { sparePartStockId = Part.Id });
    }

    private async Task LoadPageAsync()
    {
        Part = await _db.SparePartStocks.FirstOrDefaultAsync(x => x.Id == Id);

        var inventoryItems = await _db.InventoryItems
            .Where(x => x.IsActive)
            .Include(x => x.Room)
            .Include(x => x.InventoryCategory)
            .OrderBy(x => x.Room!.SortOrder)
            .ThenBy(x => x.Room!.Name)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .Select(x => new
            {
                x.Id,
                DisplayName =
                    (x.Room != null ? x.Room.Name : "Χωρίς χώρο") + " · " +
                    (x.InventoryCategory != null ? x.InventoryCategory.Name + " · " : "") +
                    x.Name +
                    (x.Brand != null && x.Brand != "" ? " · " + x.Brand : "") +
                    (x.Model != null && x.Model != "" ? " " + x.Model : "") +
                    (x.SerialNumber != null && x.SerialNumber != "" ? " · SN: " + x.SerialNumber : "")
            })
            .ToListAsync();

        InventoryItemOptions = new SelectList(inventoryItems, "Id", "DisplayName", Input.InventoryItemId);
    }

    private static string BuildSparePartSnapshot(SparePartStock part)
    {
        var pieces = new List<string>
        {
            SparePartStock.GetPartTypeLabel(part.PartType),
            part.Name
        };

        if (!string.IsNullOrWhiteSpace(part.Manufacturer))
        {
            pieces.Add(part.Manufacturer);
        }

        if (!string.IsNullOrWhiteSpace(part.ModelName))
        {
            pieces.Add(part.ModelName);
        }

        if (!string.IsNullOrWhiteSpace(part.Specification))
        {
            pieces.Add(part.Specification);
        }

        return string.Join(" · ", pieces);
    }

    private static string BuildInventoryItemSnapshot(InventoryItem item)
    {
        var pieces = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.Room?.Name))
        {
            pieces.Add(item.Room.Name);
        }

        if (!string.IsNullOrWhiteSpace(item.InventoryCategory?.Name))
        {
            pieces.Add(item.InventoryCategory.Name);
        }

        pieces.Add(item.Name);

        if (!string.IsNullOrWhiteSpace(item.Brand))
        {
            pieces.Add(item.Brand);
        }

        if (!string.IsNullOrWhiteSpace(item.Model))
        {
            pieces.Add(item.Model);
        }

        if (!string.IsNullOrWhiteSpace(item.SerialNumber))
        {
            pieces.Add("SN: " + item.SerialNumber);
        }

        return string.Join(" · ", pieces);
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public class SparePartUsageInput
    {
        public int? InventoryItemId { get; set; }

        public int QuantityUsed { get; set; } = 1;

        public DateTime UsedAt { get; set; } = DateTime.Now;

        public string? UsedBy { get; set; }

        public string? TargetDescription { get; set; }

        public string? Notes { get; set; }
    }
}
