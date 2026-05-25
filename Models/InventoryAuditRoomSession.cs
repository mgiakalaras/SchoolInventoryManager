using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class InventoryAuditRoomSession
{
    public int Id { get; set; }

    public int InventoryAuditFolderId { get; set; }
    public InventoryAuditFolder? InventoryAuditFolder { get; set; }

    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    [Display(Name = "Χώρος")]
    [StringLength(220)]
    public string RoomNameSnapshot { get; set; } = string.Empty;

    public int ExpectedItemsCount { get; set; }

    public int FoundItemsCount { get; set; }

    public int MissingItemsCount { get; set; }

    public int WrongRoomItemsCount { get; set; }

    public int UnknownItemsCount { get; set; }

    public bool IsFinalized { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<InventoryAuditScanLog> ScanLogs { get; set; } = new List<InventoryAuditScanLog>();

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
