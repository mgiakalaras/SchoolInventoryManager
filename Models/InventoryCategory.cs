using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class InventoryCategory
{
    public int Id { get; set; }

    [Display(Name = "Κατηγορία")]
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 100;

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}
