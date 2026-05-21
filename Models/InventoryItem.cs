using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class InventoryItem
{
    public int Id { get; set; }

    [Display(Name = "Χώρος")]
    [Required]
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    [Display(Name = "Κατηγορία")]
    public int? InventoryCategoryId { get; set; }
    public InventoryCategory? InventoryCategory { get; set; }

    [Display(Name = "Αντικείμενο")]
    [Required(ErrorMessage = "Συμπλήρωσε το αντικείμενο.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Ποσότητα")]
    [Range(1, 10000, ErrorMessage = "Η ποσότητα πρέπει να είναι τουλάχιστον 1.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Μάρκα")]
    [StringLength(120)]
    public string? Brand { get; set; }

    [Display(Name = "Μοντέλο")]
    [StringLength(120)]
    public string? Model { get; set; }

    [Display(Name = "Serial Number")]
    [StringLength(180)]
    public string? SerialNumber { get; set; }

    [Display(Name = "Σελίδα βιβλίου υλικού")]
    [StringLength(80)]
    public string? InventoryBookPage { get; set; }

    [Display(Name = "Περιγραφή")]
    public string? Description { get; set; }

    [Display(Name = "Κατάσταση")]
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Unknown;

    [Display(Name = "Παρατηρήσεις")]
    public string? Notes { get; set; }

    [Display(Name = "Ενεργό")]
    public bool IsActive { get; set; } = true;

    public int? DestructionBatchId { get; set; }
    public DestructionBatch? DestructionBatch { get; set; }

    public DateTime? DestroyedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
