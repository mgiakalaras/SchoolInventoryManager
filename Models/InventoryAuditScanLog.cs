using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class InventoryAuditScanLog
{
    public int Id { get; set; }

    public int InventoryAuditRoomSessionId { get; set; }
    public InventoryAuditRoomSession? InventoryAuditRoomSession { get; set; }

    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    [StringLength(64)]
    public string ScannedCode { get; set; } = string.Empty;

    [StringLength(30)]
    public string Status { get; set; } = AuditScanStatus.Unknown;

    [StringLength(220)]
    public string? ItemNameSnapshot { get; set; }

    [StringLength(220)]
    public string? ExpectedRoomSnapshot { get; set; }

    [StringLength(220)]
    public string? ActualRoomSnapshot { get; set; }

    [StringLength(220)]
    public string? CategorySnapshot { get; set; }

    [StringLength(180)]
    public string? SerialNumberSnapshot { get; set; }

    public DateTime ScannedAt { get; set; } = DateTime.Now;

    public string? Notes { get; set; }
}

public static class AuditScanStatus
{
    public const string Found = "Found";
    public const string WrongRoom = "WrongRoom";
    public const string Unknown = "Unknown";
}
