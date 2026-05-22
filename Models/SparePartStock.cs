using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class SparePartStock
{
    public int Id { get; set; }

    [Display(Name = "Τύπος")]
    [Required(ErrorMessage = "Συμπλήρωσε τύπο ανταλλακτικού/αναλώσιμου.")]
    [StringLength(80)]
    public string PartType { get; set; } = "Other";

    [Display(Name = "Ονομασία")]
    [Required(ErrorMessage = "Συμπλήρωσε ονομασία.")]
    [StringLength(220)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Κατασκευαστής")]
    [StringLength(120)]
    public string? Manufacturer { get; set; }

    [Display(Name = "Μοντέλο")]
    [StringLength(160)]
    public string? ModelName { get; set; }

    [Display(Name = "Προδιαγραφή")]
    [StringLength(260)]
    public string? Specification { get; set; }

    [Display(Name = "Ποσότητα")]
    [Range(0, 100000, ErrorMessage = "Η ποσότητα δεν μπορεί να είναι αρνητική.")]
    public int Quantity { get; set; }

    [Display(Name = "Ελάχιστο απόθεμα")]
    [Range(0, 100000, ErrorMessage = "Το ελάχιστο απόθεμα δεν μπορεί να είναι αρνητικό.")]
    public int MinimumStock { get; set; }

    [Display(Name = "Κατάσταση")]
    [StringLength(80)]
    public string? Condition { get; set; } = "Διαθέσιμο";

    [Display(Name = "Θέση αποθήκευσης")]
    [StringLength(160)]
    public string? StorageLocation { get; set; }

    [Display(Name = "Συμβατότητα")]
    [StringLength(260)]
    public string? CompatibleWith { get; set; }

    [Display(Name = "Σημειώσεις")]
    public string? Notes { get; set; }

    [Display(Name = "Ενεργό")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<SparePartUsageLog> UsageLogs { get; set; } = new List<SparePartUsageLog>();

    public bool IsLowStock => IsActive && MinimumStock > 0 && Quantity <= MinimumStock;

    public static IReadOnlyList<string> PartTypes { get; } = new[]
    {
        "RAM",
        "CPU",
        "Storage",
        "PowerSupply",
        "LaptopCharger",
        "Keyboard",
        "Mouse",
        "Cable",
        "Network",
        "Battery",
        "UPS",
        "DisplayPart",
        "Other"
    };

    public static string GetPartTypeLabel(string? partType)
    {
        return partType switch
        {
            "RAM" => "Μνήμες RAM",
            "CPU" => "Επεξεργαστές",
            "Storage" => "Δίσκοι / SSD / NVMe",
            "PowerSupply" => "Τροφοδοτικά",
            "LaptopCharger" => "Φορτιστές laptop",
            "Keyboard" => "Πληκτρολόγια",
            "Mouse" => "Ποντίκια",
            "Cable" => "Καλώδια / αντάπτορες",
            "Network" => "Δικτυακά ανταλλακτικά",
            "Battery" => "Μπαταρίες",
            "UPS" => "UPS / μπαταρίες UPS",
            "DisplayPart" => "Ανταλλακτικά οθονών / διαδραστικών",
            _ => "Λοιπά"
        };
    }
}
