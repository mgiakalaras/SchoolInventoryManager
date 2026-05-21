using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Destructions;

public class FinalizeModel : PageModel
{
    private readonly AppDbContext _db;

    public FinalizeModel(AppDbContext db)
    {
        _db = db;
    }

    public DestructionBatch? Batch { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Batch = await LoadBatchAsync(id);
        return Batch == null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Batch = await LoadBatchAsync(id);
        if (Batch == null)
        {
            return NotFound();
        }

        if (Batch.IsFinalized)
        {
            ModelState.AddModelError(string.Empty, "Ο φάκελος είναι ήδη οριστικοποιημένος.");
            return Page();
        }

        if (!Batch.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Δεν υπάρχουν υλικά στον φάκελο.");
            return Page();
        }

        if (Batch.CommitteeMembers.Count(x => !string.IsNullOrWhiteSpace(x.FullName)) < 3)
        {
            ModelState.AddModelError(string.Empty, "Συμπλήρωσε τουλάχιστον 3 μέλη επιτροπής πριν την οριστικοποίηση.");
            return Page();
        }

        var now = DateTime.Now;

        foreach (var batchItem in Batch.Items)
        {
            var item = batchItem.InventoryItem;
            if (item == null)
            {
                continue;
            }

            item.Condition = EquipmentCondition.Destroyed;
            item.IsActive = false;
            item.DestructionBatchId = Batch.Id;
            item.DestroyedAt = now;
            item.UpdatedAt = now;
        }

        Batch.IsFinalized = true;
        Batch.FinalizedAt = now;
        Batch.UpdatedAt = now;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Η καταστροφή οριστικοποιήθηκε. Τα υλικά αφαιρέθηκαν από την ενεργή απογραφή.";
        return RedirectToPage("Edit", new { id });
    }

    private async Task<DestructionBatch?> LoadBatchAsync(int id)
    {
        return await _db.DestructionBatches
            .Include(x => x.Items)
                .ThenInclude(x => x.InventoryItem)
            .Include(x => x.CommitteeMembers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
