using Microsoft.EntityFrameworkCore;

namespace SchoolInventoryManager.Data;

public static class SchemaUpgrade
{
    public static void Apply(AppDbContext db)
    {
        AddColumnIfMissing(db, "InventoryItems", "InventoryBookPage", "TEXT NULL");
        AddColumnIfMissing(db, "InventoryItems", "DestructionBatchId", "INTEGER NULL");
        AddColumnIfMissing(db, "InventoryItems", "DestroyedAt", "TEXT NULL");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS DestructionBatches (
    Id INTEGER NOT NULL CONSTRAINT PK_DestructionBatches PRIMARY KEY AUTOINCREMENT,
    ActNumber TEXT NULL,
    ActDate TEXT NOT NULL,
    ProtocolNumber TEXT NULL,
    ProtocolDate TEXT NOT NULL,
    SchoolName TEXT NOT NULL,
    Location TEXT NULL,
    MeetingDayName TEXT NULL,
    MeetingTime TEXT NULL,
    RecommenderName TEXT NULL,
    RecommenderTitle TEXT NULL,
    ChairpersonName TEXT NULL,
    Notes TEXT NULL,
    IsFinalized INTEGER NOT NULL DEFAULT 0,
    FinalizedAt TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS DestructionBatchItems (
    Id INTEGER NOT NULL CONSTRAINT PK_DestructionBatchItems PRIMARY KEY AUTOINCREMENT,
    DestructionBatchId INTEGER NOT NULL,
    InventoryItemId INTEGER NOT NULL,
    ItemNameSnapshot TEXT NOT NULL,
    BrandSnapshot TEXT NULL,
    ModelSnapshot TEXT NULL,
    SerialNumberSnapshot TEXT NULL,
    RoomSnapshot TEXT NULL,
    CategorySnapshot TEXT NULL,
    RegistryBookPageSnapshot TEXT NULL,
    QuantitySnapshot INTEGER NOT NULL DEFAULT 1,
    NotesSnapshot TEXT NULL,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT FK_DestructionBatchItems_DestructionBatches_DestructionBatchId FOREIGN KEY (DestructionBatchId) REFERENCES DestructionBatches (Id) ON DELETE CASCADE,
    CONSTRAINT FK_DestructionBatchItems_InventoryItems_InventoryItemId FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems (Id) ON DELETE RESTRICT
);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS DestructionCommitteeMembers (
    Id INTEGER NOT NULL CONSTRAINT PK_DestructionCommitteeMembers PRIMARY KEY AUTOINCREMENT,
    DestructionBatchId INTEGER NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NULL,
    SortOrder INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT FK_DestructionCommitteeMembers_DestructionBatches_DestructionBatchId FOREIGN KEY (DestructionBatchId) REFERENCES DestructionBatches (Id) ON DELETE CASCADE
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DestructionBatchItems_DestructionBatchId ON DestructionBatchItems (DestructionBatchId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DestructionBatchItems_InventoryItemId ON DestructionBatchItems (InventoryItemId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DestructionCommitteeMembers_DestructionBatchId ON DestructionCommitteeMembers (DestructionBatchId);");
    }

    private static void AddColumnIfMissing(AppDbContext db, string tableName, string columnName, string columnDefinition)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            var exists = false;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName});";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
            {
                db.Database.ExecuteSqlRaw($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
            }
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
