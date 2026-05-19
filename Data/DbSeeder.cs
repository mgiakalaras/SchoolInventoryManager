using SchoolInventoryManager.Models;

namespace SchoolInventoryManager.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (!db.SchoolSettings.Any())
        {
            db.SchoolSettings.Add(new SchoolSettings
            {
                Id = 1,
                SchoolName = "Το σχολείο μου",
                SchoolType = "Γενικό Λύκειο",
                SchoolYear = $"{DateTime.Today.Year - 1}-{DateTime.Today.Year}",
                InventoryDate = DateTime.Today,
                InventoryManagerTitle = "Υπεύθυνος/η απογραφής",
                GeneralNotes = "Η παρούσα απογραφή αποτελεί προσπάθεια καταγραφής του ψηφιακού εξοπλισμού της σχολικής μονάδας. Τα στοιχεία μπορούν να επικαιροποιούνται όταν μεταβάλλεται ο εξοπλισμός ή η κατάστασή του."
            });
        }

        if (!db.InventoryCategories.Any())
        {
            db.InventoryCategories.AddRange(
                new InventoryCategory { Name = "Η/Υ", SortOrder = 1 },
                new InventoryCategory { Name = "Οθόνη", SortOrder = 2 },
                new InventoryCategory { Name = "Πληκτρολόγιο", SortOrder = 3 },
                new InventoryCategory { Name = "Ποντίκι", SortOrder = 4 },
                new InventoryCategory { Name = "Εκτυπωτής", SortOrder = 5 },
                new InventoryCategory { Name = "Βιντεοπροβολέας", SortOrder = 6 },
                new InventoryCategory { Name = "Tablet", SortOrder = 7 },
                new InventoryCategory { Name = "Δικτυακός εξοπλισμός", SortOrder = 8 },
                new InventoryCategory { Name = "Ήχος / Μικρόφωνο", SortOrder = 9 },
                new InventoryCategory { Name = "Καλώδια / Περιφερειακά", SortOrder = 10 },
                new InventoryCategory { Name = "Άλλο", SortOrder = 99 }
            );
        }

        if (!db.Rooms.Any())
        {
            db.Rooms.AddRange(
                new Room { Name = "Γραφείο Καθηγητών", RoomType = "Γραφείο", SortOrder = 1 },
                new Room { Name = "Γραφείο Διεύθυνσης", RoomType = "Γραφείο", SortOrder = 2 },
                new Room { Name = "Γραμματεία", RoomType = "Γραφείο", SortOrder = 3 },
                new Room { Name = "Εργαστήριο Πληροφορικής", RoomType = "Εργαστήριο", SortOrder = 10 },
                new Room { Name = "Βιβλιοθήκη", RoomType = "Αίθουσα", SortOrder = 20 }
            );
        }

        db.SaveChanges();
    }
}
