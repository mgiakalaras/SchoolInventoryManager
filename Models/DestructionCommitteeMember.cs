using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class DestructionCommitteeMember
{
    public int Id { get; set; }

    public int DestructionBatchId { get; set; }
    public DestructionBatch? DestructionBatch { get; set; }

    [Display(Name = "Ονοματεπώνυμο")]
    [StringLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Ιδιότητα / ρόλος")]
    [StringLength(180)]
    public string? Role { get; set; }

    public int SortOrder { get; set; } = 1;
}
