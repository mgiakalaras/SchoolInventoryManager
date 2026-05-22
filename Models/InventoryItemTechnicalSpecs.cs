using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class InventoryItemTechnicalSpecs
{
    public int Id { get; set; }

    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    [Display(Name = "Επεξεργαστής")]
    [StringLength(160)]
    public string? Processor { get; set; }

    [Display(Name = "Μνήμη RAM")]
    [StringLength(120)]
    public string? MemoryRam { get; set; }

    [Display(Name = "Τύπος μνήμης")]
    [StringLength(80)]
    public string? MemoryType { get; set; }

    [Display(Name = "Αποθήκευση")]
    [StringLength(160)]
    public string? Storage { get; set; }

    [Display(Name = "Τύπος δίσκου")]
    [StringLength(80)]
    public string? StorageType { get; set; }

    [Display(Name = "Κάρτα γραφικών")]
    [StringLength(160)]
    public string? Graphics { get; set; }

    [Display(Name = "Λειτουργικό σύστημα")]
    [StringLength(160)]
    public string? OperatingSystem { get; set; }

    [Display(Name = "Άδεια / COA")]
    [StringLength(160)]
    public string? LicenseInfo { get; set; }

    [Display(Name = "Δίκτυο")]
    [StringLength(160)]
    public string? NetworkInfo { get; set; }

    [Display(Name = "OPS / Mini PC module")]
    [StringLength(160)]
    public string? OpsModuleModel { get; set; }

    [Display(Name = "Τεχνικές σημειώσεις")]
    public string? TechnicalNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool HasAnyValue()
    {
        return !string.IsNullOrWhiteSpace(Processor)
            || !string.IsNullOrWhiteSpace(MemoryRam)
            || !string.IsNullOrWhiteSpace(MemoryType)
            || !string.IsNullOrWhiteSpace(Storage)
            || !string.IsNullOrWhiteSpace(StorageType)
            || !string.IsNullOrWhiteSpace(Graphics)
            || !string.IsNullOrWhiteSpace(OperatingSystem)
            || !string.IsNullOrWhiteSpace(LicenseInfo)
            || !string.IsNullOrWhiteSpace(NetworkInfo)
            || !string.IsNullOrWhiteSpace(OpsModuleModel)
            || !string.IsNullOrWhiteSpace(TechnicalNotes);
    }
}
