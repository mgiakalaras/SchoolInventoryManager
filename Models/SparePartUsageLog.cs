using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class SparePartUsageLog
{
    public int Id { get; set; }

    [Display(Name = "Ανταλλακτικό / αναλώσιμο")]
    public int SparePartStockId { get; set; }

    public SparePartStock? SparePartStock { get; set; }

    [Display(Name = "Συσκευή / υλικό")]
    public int? InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    [Display(Name = "Ποσότητα")]
    [Range(1, 100000, ErrorMessage = "Η ποσότητα πρέπει να είναι τουλάχιστον 1.")]
    public int QuantityUsed { get; set; } = 1;

    [Display(Name = "Ημερομηνία χρήσης")]
    public DateTime UsedAt { get; set; } = DateTime.Now;

    [Display(Name = "Χρήστης / τεχνικός")]
    [StringLength(160)]
    public string? UsedBy { get; set; }

    [Display(Name = "Snapshot ανταλλακτικού")]
    [StringLength(500)]
    public string SparePartSnapshot { get; set; } = string.Empty;

    [Display(Name = "Snapshot συσκευής")]
    [StringLength(700)]
    public string? TargetDescriptionSnapshot { get; set; }

    [Display(Name = "Σημειώσεις")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
