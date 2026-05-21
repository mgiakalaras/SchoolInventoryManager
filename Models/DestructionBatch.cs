using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public class DestructionBatch
{
    public int Id { get; set; }

    [Display(Name = "Αριθμός πράξης")]
    [StringLength(80)]
    public string? ActNumber { get; set; }

    [Display(Name = "Ημερομηνία πράξης")]
    [DataType(DataType.Date)]
    public DateTime ActDate { get; set; } = DateTime.Today;

    [Display(Name = "Αριθμός πρωτοκόλλου")]
    [StringLength(80)]
    public string? ProtocolNumber { get; set; }

    [Display(Name = "Ημερομηνία πρωτοκόλλου")]
    [DataType(DataType.Date)]
    public DateTime ProtocolDate { get; set; } = DateTime.Today;

    [Display(Name = "Σχολική μονάδα")]
    [StringLength(200)]
    public string SchoolName { get; set; } = string.Empty;

    [Display(Name = "Τόπος συνεδρίασης / καταστροφής")]
    [StringLength(200)]
    public string? Location { get; set; }

    [Display(Name = "Ημέρα")]
    [StringLength(60)]
    public string? MeetingDayName { get; set; }

    [Display(Name = "Ώρα")]
    [StringLength(30)]
    public string? MeetingTime { get; set; }

    [Display(Name = "Εισηγητής / Διευθυντής")]
    [StringLength(180)]
    public string? RecommenderName { get; set; }

    [Display(Name = "Ιδιότητα εισηγητή")]
    [StringLength(180)]
    public string? RecommenderTitle { get; set; }

    [Display(Name = "Πρόεδρος σχολικής επιτροπής")]
    [StringLength(180)]
    public string? ChairpersonName { get; set; }

    [Display(Name = "Αιτιολογία / παρατηρήσεις")]
    public string? Notes { get; set; }

    public bool IsFinalized { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<DestructionBatchItem> Items { get; set; } = new List<DestructionBatchItem>();

    public ICollection<DestructionCommitteeMember> CommitteeMembers { get; set; } = new List<DestructionCommitteeMember>();
}
