using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class DestructionBatchItem
{
    public int Id { get; set; }

    public int DestructionBatchId { get; set; }
    public DestructionBatch? DestructionBatch { get; set; }

    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    [StringLength(250)]
    public string ItemNameSnapshot { get; set; } = string.Empty;

    [StringLength(160)]
    public string? BrandSnapshot { get; set; }

    [StringLength(160)]
    public string? ModelSnapshot { get; set; }

    [StringLength(200)]
    public string? SerialNumberSnapshot { get; set; }

    [StringLength(200)]
    public string? RoomSnapshot { get; set; }

    [StringLength(160)]
    public string? CategorySnapshot { get; set; }

    [StringLength(80)]
    public string? RegistryBookPageSnapshot { get; set; }

    public int QuantitySnapshot { get; set; } = 1;

    public string? NotesSnapshot { get; set; }

    public int SortOrder { get; set; }
}
