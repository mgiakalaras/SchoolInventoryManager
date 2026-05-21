using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Data;
using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Pages.Destructions;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public DestructionBatch Batch { get; set; } = new();

    [BindProperty]
    public List<CommitteeInput> CommitteeMembers { get; set; } = new();

    public IList<DestructionBatchItem> Items { get; set; } = new List<DestructionBatchItem>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var batch = await LoadBatchAsync(id);
        if (batch == null)
        {
            return NotFound();
        }

        Batch = batch;
        CommitteeMembers = batch.CommitteeMembers
            .OrderBy(x => x.SortOrder)
            .Select(x => new CommitteeInput
            {
                Id = x.Id,
                FullName = x.FullName,
                Role = x.Role,
                SortOrder = x.SortOrder
            })
            .ToList();

        while (CommitteeMembers.Count < 3)
        {
            CommitteeMembers.Add(new CommitteeInput { SortOrder = CommitteeMembers.Count + 1, Role = "Μέλος επιτροπής" });
        }

        Items = batch.Items.OrderBy(x => x.SortOrder).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _db.DestructionBatches
            .Include(x => x.CommitteeMembers)
            .FirstOrDefaultAsync(x => x.Id == Batch.Id);

        if (existing == null)
        {
            return NotFound();
        }

        if (existing.IsFinalized)
        {
            ModelState.AddModelError(string.Empty, "Ο φάκελος έχει οριστικοποιηθεί και δεν μπορεί να αλλάξει.");
        }

        if (string.IsNullOrWhiteSpace(Batch.SchoolName))
        {
            ModelState.AddModelError("Batch.SchoolName", "Συμπλήρωσε σχολική μονάδα.");
        }

        if (!ModelState.IsValid)
        {
            var loaded = await LoadBatchAsync(Batch.Id);
            Items = loaded?.Items.OrderBy(x => x.SortOrder).ToList() ?? new List<DestructionBatchItem>();
            return Page();
        }

        existing.ActNumber = Batch.ActNumber?.Trim();
        existing.ActDate = Batch.ActDate;
        existing.ProtocolNumber = Batch.ProtocolNumber?.Trim();
        existing.ProtocolDate = Batch.ProtocolDate;
        existing.SchoolName = Batch.SchoolName.Trim();
        existing.Location = Batch.Location?.Trim();
        existing.MeetingDayName = Batch.MeetingDayName?.Trim();
        existing.MeetingTime = Batch.MeetingTime?.Trim();
        existing.RecommenderName = Batch.RecommenderName?.Trim();
        existing.RecommenderTitle = Batch.RecommenderTitle?.Trim();
        existing.ChairpersonName = Batch.ChairpersonName?.Trim();
        existing.Notes = Batch.Notes?.Trim();
        existing.UpdatedAt = DateTime.Now;

        _db.DestructionCommitteeMembers.RemoveRange(existing.CommitteeMembers);

        foreach (var member in CommitteeMembers
            .Where(x => !string.IsNullOrWhiteSpace(x.FullName))
            .OrderBy(x => x.SortOrder))
        {
            existing.CommitteeMembers.Add(new DestructionCommitteeMember
            {
                FullName = member.FullName.Trim(),
                Role = member.Role?.Trim(),
                SortOrder = member.SortOrder
            });
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = "Ο φάκελος καταστροφής αποθηκεύτηκε.";
        return RedirectToPage("Edit", new { id = existing.Id });
    }

    private async Task<DestructionBatch?> LoadBatchAsync(int id)
    {
        return await _db.DestructionBatches
            .Include(x => x.Items)
                .ThenInclude(x => x.InventoryItem)
            .Include(x => x.CommitteeMembers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public class CommitteeInput
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public int SortOrder { get; set; } = 1;
    }
}
