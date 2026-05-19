using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class Room
{
    public int Id { get; set; }

    [Display(Name = "Χώρος / Αίθουσα")]
    [Required(ErrorMessage = "Συμπλήρωσε όνομα χώρου.")]
    [StringLength(180)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Όροφος")]
    [StringLength(80)]
    public string? Floor { get; set; }

    [Display(Name = "Τύπος χώρου")]
    [StringLength(100)]
    public string? RoomType { get; set; }

    [Display(Name = "Εσωτερική σειρά")]
    public int SortOrder { get; set; } = 100;

    [Display(Name = "Σημειώσεις")]
    public string? Notes { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}
