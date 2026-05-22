using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class TechnicalReference
{
    public int Id { get; set; }

    [Display(Name = "Τύπος αναφοράς")]
    [Required]
    [StringLength(80)]
    public string ReferenceType { get; set; } = "Processor";

    [Display(Name = "Εμφάνιση")]
    [Required(ErrorMessage = "Συμπλήρωσε την ονομασία αναφοράς.")]
    [StringLength(220)]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Κατασκευαστής")]
    [StringLength(120)]
    public string? Manufacturer { get; set; }

    [Display(Name = "Σειρά")]
    [StringLength(120)]
    public string? Series { get; set; }

    [Display(Name = "Μοντέλο")]
    [StringLength(160)]
    public string? ModelName { get; set; }

    [Display(Name = "Λεπτομέρειες")]
    [StringLength(260)]
    public string? Detail { get; set; }

    [Display(Name = "Έτος")]
    public int? ApproxYear { get; set; }

    [Display(Name = "Σειρά εμφάνισης")]
    public int SortOrder { get; set; }

    [Display(Name = "Ενεργό")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ενσωματωμένη εγγραφή")]
    public bool IsBuiltIn { get; set; }

    [Display(Name = "Σημειώσεις")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
