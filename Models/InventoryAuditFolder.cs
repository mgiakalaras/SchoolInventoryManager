using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class InventoryAuditFolder
{
    public int Id { get; set; }

    [Display(Name = "Τίτλος φακέλου")]
    [Required(ErrorMessage = "Συμπλήρωσε τίτλο φακέλου.")]
    [StringLength(220)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Ημερομηνία απογραφής")]
    [DataType(DataType.Date)]
    public DateTime AuditDate { get; set; } = DateTime.Today;

    [Display(Name = "Σχολικό έτος")]
    [StringLength(40)]
    public string? SchoolYear { get; set; }

    [Display(Name = "Σχολική μονάδα")]
    [StringLength(220)]
    public string SchoolName { get; set; } = string.Empty;

    [Display(Name = "Τύπος σχολείου")]
    [StringLength(120)]
    public string? SchoolType { get; set; }

    [Display(Name = "Υπεύθυνος/η απογραφής")]
    [StringLength(180)]
    public string? ResponsibleName { get; set; }

    [Display(Name = "Παρατηρήσεις")]
    public string? Notes { get; set; }

    [Display(Name = "Οριστικοποιημένος")]
    public bool IsFinalized { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<InventoryAuditRoomSession> RoomSessions { get; set; } = new List<InventoryAuditRoomSession>();
}
