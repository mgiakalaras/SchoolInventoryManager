using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class SchoolSettings
{
    public int Id { get; set; } = 1;

    [Display(Name = "Όνομα σχολείου")]
    [Required(ErrorMessage = "Συμπλήρωσε το όνομα σχολείου.")]
    [StringLength(200)]
    public string SchoolName { get; set; } = "Το σχολείο μου";

    [Display(Name = "Τύπος σχολείου")]
    [StringLength(150)]
    public string? SchoolType { get; set; } = "Γενικό Λύκειο";

    [Display(Name = "Περιοχή / Διεύθυνση")]
    [StringLength(250)]
    public string? Address { get; set; }

    [Display(Name = "Σχολικό έτος")]
    [StringLength(20)]
    public string SchoolYear { get; set; } = "2025-2026";

    [Display(Name = "Ημερομηνία απογραφής")]
    [DataType(DataType.Date)]
    public DateTime InventoryDate { get; set; } = DateTime.Today;

    [Display(Name = "Υπεύθυνος απογραφής")]
    [StringLength(150)]
    public string? InventoryManagerName { get; set; }

    [Display(Name = "Ιδιότητα υπεύθυνου")]
    [StringLength(150)]
    public string? InventoryManagerTitle { get; set; } = "Υπεύθυνος/η απογραφής";

    [Display(Name = "Διευθυντής/Διευθύντρια")]
    [StringLength(150)]
    public string? PrincipalName { get; set; }

    [Display(Name = "Γενικές παρατηρήσεις")]
    public string? GeneralNotes { get; set; }

    [Display(Name = "Λογότυπο")]
    public string? LogoPath { get; set; }
}
