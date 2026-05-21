using System.ComponentModel.DataAnnotations;

namespace SchoolInventoryManager.Models;

public enum EquipmentCondition
{
    [Display(Name = "Άγνωστη")]
    Unknown = 0,

    [Display(Name = "Λειτουργικό")]
    Working = 1,

    [Display(Name = "Χρειάζεται έλεγχο")]
    NeedsCheck = 2,

    [Display(Name = "Χαλασμένο")]
    Broken = 3,

    [Display(Name = "Προς απόσυρση")]
    ToWithdraw = 4,

    [Display(Name = "Αποθηκευμένο")]
    Stored = 5,

    [Display(Name = "Κατεστραμμένο / αποσυρμένο")]
    Destroyed = 6
}
