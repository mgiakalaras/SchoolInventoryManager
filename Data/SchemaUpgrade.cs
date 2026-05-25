using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Data;

public static class SchemaUpgrade
{
    public static void Apply(AppDbContext db)
    {
        AddColumnIfMissing(db, "SchoolSettings", "ApplicationBaseUrl", "TEXT NULL");
        AddColumnIfMissing(db, "InventoryAuditFolders", "SchoolType", "TEXT NULL");

        AddColumnIfMissing(db, "InventoryItems", "InventoryBookPage", "TEXT NULL");
        AddColumnIfMissing(db, "InventoryItems", "DestructionBatchId", "INTEGER NULL");
        AddColumnIfMissing(db, "InventoryItems", "DestroyedAt", "TEXT NULL");
        AddColumnIfMissing(db, "InventoryItems", "AssetCode", "TEXT NULL");
        AddColumnIfMissing(db, "InventoryItems", "QrToken", "TEXT NULL");

        BackfillInventoryQrIdentity(db);

        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_InventoryItems_AssetCode ON InventoryItems (AssetCode);");
        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_InventoryItems_QrToken ON InventoryItems (QrToken);");


        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS InventoryAuditFolders (
    Id INTEGER NOT NULL CONSTRAINT PK_InventoryAuditFolders PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    AuditDate TEXT NOT NULL,
    SchoolYear TEXT NULL,
    SchoolName TEXT NOT NULL,
    SchoolType TEXT NULL,
    ResponsibleName TEXT NULL,
    Notes TEXT NULL,
    IsFinalized INTEGER NOT NULL DEFAULT 0,
    FinalizedAt TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InventoryAuditFolders_AuditDate_IsFinalized ON InventoryAuditFolders (AuditDate, IsFinalized);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS InventoryAuditRoomSessions (
    Id INTEGER NOT NULL CONSTRAINT PK_InventoryAuditRoomSessions PRIMARY KEY AUTOINCREMENT,
    InventoryAuditFolderId INTEGER NOT NULL,
    RoomId INTEGER NULL,
    RoomNameSnapshot TEXT NOT NULL,
    ExpectedItemsCount INTEGER NOT NULL DEFAULT 0,
    FoundItemsCount INTEGER NOT NULL DEFAULT 0,
    MissingItemsCount INTEGER NOT NULL DEFAULT 0,
    WrongRoomItemsCount INTEGER NOT NULL DEFAULT 0,
    UnknownItemsCount INTEGER NOT NULL DEFAULT 0,
    IsFinalized INTEGER NOT NULL DEFAULT 0,
    StartedAt TEXT NULL,
    CompletedAt TEXT NULL,
    Notes TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    CONSTRAINT FK_InventoryAuditRoomSessions_InventoryAuditFolders_InventoryAuditFolderId FOREIGN KEY (InventoryAuditFolderId) REFERENCES InventoryAuditFolders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_InventoryAuditRoomSessions_Rooms_RoomId FOREIGN KEY (RoomId) REFERENCES Rooms (Id) ON DELETE SET NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InventoryAuditRoomSessions_Folder_Room ON InventoryAuditRoomSessions (InventoryAuditFolderId, RoomId);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS InventoryAuditScanLogs (
    Id INTEGER NOT NULL CONSTRAINT PK_InventoryAuditScanLogs PRIMARY KEY AUTOINCREMENT,
    InventoryAuditRoomSessionId INTEGER NOT NULL,
    InventoryItemId INTEGER NULL,
    ScannedCode TEXT NOT NULL,
    Status TEXT NOT NULL,
    ItemNameSnapshot TEXT NULL,
    ExpectedRoomSnapshot TEXT NULL,
    ActualRoomSnapshot TEXT NULL,
    CategorySnapshot TEXT NULL,
    SerialNumberSnapshot TEXT NULL,
    ScannedAt TEXT NOT NULL,
    Notes TEXT NULL,
    CONSTRAINT FK_InventoryAuditScanLogs_InventoryAuditRoomSessions_InventoryAuditRoomSessionId FOREIGN KEY (InventoryAuditRoomSessionId) REFERENCES InventoryAuditRoomSessions (Id) ON DELETE CASCADE,
    CONSTRAINT FK_InventoryAuditScanLogs_InventoryItems_InventoryItemId FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems (Id) ON DELETE SET NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InventoryAuditScanLogs_Session_Status ON InventoryAuditScanLogs (InventoryAuditRoomSessionId, Status);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InventoryAuditScanLogs_Session_Item ON InventoryAuditScanLogs (InventoryAuditRoomSessionId, InventoryItemId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InventoryAuditScanLogs_Session_Code ON InventoryAuditScanLogs (InventoryAuditRoomSessionId, ScannedCode);");




        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS InventoryItemTechnicalSpecs (
    Id INTEGER NOT NULL CONSTRAINT PK_InventoryItemTechnicalSpecs PRIMARY KEY AUTOINCREMENT,
    InventoryItemId INTEGER NOT NULL,
    Processor TEXT NULL,
    MemoryRam TEXT NULL,
    MemoryType TEXT NULL,
    Storage TEXT NULL,
    StorageType TEXT NULL,
    Graphics TEXT NULL,
    OperatingSystem TEXT NULL,
    LicenseInfo TEXT NULL,
    NetworkInfo TEXT NULL,
    OpsModuleModel TEXT NULL,
    TechnicalNotes TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    CONSTRAINT FK_InventoryItemTechnicalSpecs_InventoryItems_InventoryItemId FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems (Id) ON DELETE CASCADE
);");

        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_InventoryItemTechnicalSpecs_InventoryItemId ON InventoryItemTechnicalSpecs (InventoryItemId);");


        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS TechnicalReferences (
    Id INTEGER NOT NULL CONSTRAINT PK_TechnicalReferences PRIMARY KEY AUTOINCREMENT,
    ReferenceType TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    Manufacturer TEXT NULL,
    Series TEXT NULL,
    ModelName TEXT NULL,
    Detail TEXT NULL,
    ApproxYear INTEGER NULL,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsBuiltIn INTEGER NOT NULL DEFAULT 0,
    Notes TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_TechnicalReferences_ReferenceType_DisplayName ON TechnicalReferences (ReferenceType, DisplayName);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TechnicalReferences_ReferenceType_IsActive ON TechnicalReferences (ReferenceType, IsActive);");

        SeedTechnicalReferences(db);



        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS SparePartStocks (
    Id INTEGER NOT NULL CONSTRAINT PK_SparePartStocks PRIMARY KEY AUTOINCREMENT,
    PartType TEXT NOT NULL,
    Name TEXT NOT NULL,
    Manufacturer TEXT NULL,
    ModelName TEXT NULL,
    Specification TEXT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0,
    MinimumStock INTEGER NOT NULL DEFAULT 0,
    Condition TEXT NULL,
    StorageLocation TEXT NULL,
    CompatibleWith TEXT NULL,
    Notes TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_SparePartStocks_PartType_IsActive ON SparePartStocks (PartType, IsActive);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_SparePartStocks_Name ON SparePartStocks (Name);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS SparePartUsageLogs (
    Id INTEGER NOT NULL CONSTRAINT PK_SparePartUsageLogs PRIMARY KEY AUTOINCREMENT,
    SparePartStockId INTEGER NOT NULL,
    InventoryItemId INTEGER NULL,
    QuantityUsed INTEGER NOT NULL DEFAULT 1,
    UsedAt TEXT NOT NULL,
    UsedBy TEXT NULL,
    SparePartSnapshot TEXT NOT NULL,
    TargetDescriptionSnapshot TEXT NULL,
    Notes TEXT NULL,
    CreatedAt TEXT NOT NULL,
    CONSTRAINT FK_SparePartUsageLogs_SparePartStocks_SparePartStockId FOREIGN KEY (SparePartStockId) REFERENCES SparePartStocks (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_SparePartUsageLogs_InventoryItems_InventoryItemId FOREIGN KEY (InventoryItemId) REFERENCES InventoryItems (Id) ON DELETE SET NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_SparePartUsageLogs_SparePartStockId_UsedAt ON SparePartUsageLogs (SparePartStockId, UsedAt);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_SparePartUsageLogs_InventoryItemId ON SparePartUsageLogs (InventoryItemId);");


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



    private static void BackfillInventoryQrIdentity(AppDbContext db)
    {
        var items = db.InventoryItems
            .Where(x => string.IsNullOrWhiteSpace(x.AssetCode) || string.IsNullOrWhiteSpace(x.QrToken))
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.AssetCode))
            {
                item.AssetCode = AssetCodeGenerator.CreateAssetCode(item.Id, item.CreatedAt);
            }

            if (string.IsNullOrWhiteSpace(item.QrToken))
            {
                item.QrToken = AssetCodeGenerator.CreateQrToken();
            }
        }

        db.SaveChanges();
    }


    private static void SeedTechnicalReferences(AppDbContext db)
    {
        var now = DateTime.Now.ToString("O");

        var references = new (string Type, string DisplayName, string? Manufacturer, string? Series, string? ModelName, string? Detail, int? Year, int SortOrder)[]
        {
            ("Processor", "Intel Core i3-2100", "Intel", "Core i3", "i3-2100", "2C/4T · Sandy Bridge", 2011, 10),
            ("Processor", "Intel Core i5-2400", "Intel", "Core i5", "i5-2400", "4C/4T · Sandy Bridge", 2011, 20),
            ("Processor", "Intel Core i5-3470", "Intel", "Core i5", "i5-3470", "4C/4T · Ivy Bridge", 2012, 30),
            ("Processor", "Intel Core i5-4570", "Intel", "Core i5", "i5-4570", "4C/4T · Haswell", 2013, 40),
            ("Processor", "Intel Core i5-6500", "Intel", "Core i5", "i5-6500", "4C/4T · Skylake", 2015, 50),
            ("Processor", "Intel Core i5-7500", "Intel", "Core i5", "i5-7500", "4C/4T · Kaby Lake", 2017, 60),
            ("Processor", "Intel Core i3-8100", "Intel", "Core i3", "i3-8100", "4C/4T · Coffee Lake", 2017, 70),
            ("Processor", "Intel Core i5-8400", "Intel", "Core i5", "i5-8400", "6C/6T · Coffee Lake", 2017, 80),
            ("Processor", "Intel Core i5-9400", "Intel", "Core i5", "i5-9400", "6C/6T · Coffee Lake Refresh", 2019, 90),
            ("Processor", "Intel Core i5-10400", "Intel", "Core i5", "i5-10400", "6C/12T · Comet Lake", 2020, 100),
            ("Processor", "Intel Core i5-11400", "Intel", "Core i5", "i5-11400", "6C/12T · Rocket Lake", 2021, 110),
            ("Processor", "Intel Core i5-12400", "Intel", "Core i5", "i5-12400", "6C/12T · Alder Lake", 2022, 120),
            ("Processor", "AMD Ryzen 3 2200G", "AMD", "Ryzen 3", "2200G", "4C/4T · Vega graphics", 2018, 130),
            ("Processor", "AMD Ryzen 5 2400G", "AMD", "Ryzen 5", "2400G", "4C/8T · Vega graphics", 2018, 140),
            ("Processor", "AMD Ryzen 5 2600", "AMD", "Ryzen 5", "2600", "6C/12T", 2018, 150),
            ("Processor", "AMD Ryzen 5 3400G", "AMD", "Ryzen 5", "3400G", "4C/8T · Vega graphics", 2019, 160),
            ("Processor", "AMD Ryzen 5 3600", "AMD", "Ryzen 5", "3600", "6C/12T", 2019, 170),
            ("Processor", "AMD Ryzen 5 4600G", "AMD", "Ryzen 5", "4600G", "6C/12T · Vega graphics", 2020, 180),
            ("Processor", "AMD Ryzen 5 5600G", "AMD", "Ryzen 5", "5600G", "6C/12T · Vega graphics", 2021, 190),
            ("Processor", "AMD Ryzen 5 5600", "AMD", "Ryzen 5", "5600", "6C/12T", 2022, 200),

            ("Memory", "DDR3 2GB DIMM", null, "DDR3", "2GB DIMM", "Desktop memory", 2010, 10),
            ("Memory", "DDR3 4GB DIMM", null, "DDR3", "4GB DIMM", "Desktop memory", 2010, 20),
            ("Memory", "DDR3 8GB DIMM", null, "DDR3", "8GB DIMM", "Desktop memory", 2011, 30),
            ("Memory", "DDR3 4GB SODIMM", null, "DDR3", "4GB SODIMM", "Laptop memory", 2010, 40),
            ("Memory", "DDR3 8GB SODIMM", null, "DDR3", "8GB SODIMM", "Laptop memory", 2011, 50),
            ("Memory", "DDR4 4GB DIMM", null, "DDR4", "4GB DIMM", "Desktop memory", 2014, 60),
            ("Memory", "DDR4 8GB DIMM", null, "DDR4", "8GB DIMM", "Desktop memory", 2014, 70),
            ("Memory", "DDR4 16GB DIMM", null, "DDR4", "16GB DIMM", "Desktop memory", 2014, 80),
            ("Memory", "DDR4 8GB SODIMM", null, "DDR4", "8GB SODIMM", "Laptop/mini PC memory", 2014, 90),
            ("Memory", "DDR4 16GB SODIMM", null, "DDR4", "16GB SODIMM", "Laptop/mini PC memory", 2014, 100),
            ("Memory", "DDR5 8GB DIMM", null, "DDR5", "8GB DIMM", "Desktop memory", 2021, 110),
            ("Memory", "DDR5 16GB DIMM", null, "DDR5", "16GB DIMM", "Desktop memory", 2021, 120),
            ("Memory", "DDR5 16GB SODIMM", null, "DDR5", "16GB SODIMM", "Laptop/mini PC memory", 2021, 130),

            ("MemoryType", "DDR3 DIMM", null, "DDR3", "DIMM", "Desktop memory type", 2010, 10),
            ("MemoryType", "DDR3 SODIMM", null, "DDR3", "SODIMM", "Laptop memory type", 2010, 20),
            ("MemoryType", "DDR4 DIMM", null, "DDR4", "DIMM", "Desktop memory type", 2014, 30),
            ("MemoryType", "DDR4 SODIMM", null, "DDR4", "SODIMM", "Laptop memory type", 2014, 40),
            ("MemoryType", "DDR5 DIMM", null, "DDR5", "DIMM", "Desktop memory type", 2021, 50),
            ("MemoryType", "DDR5 SODIMM", null, "DDR5", "SODIMM", "Laptop memory type", 2021, 60),

            ("Storage", "HDD SATA 250GB 3.5\"", null, "HDD", "250GB 3.5\"", "Desktop SATA hard disk", 2010, 10),
            ("Storage", "HDD SATA 500GB 3.5\"", null, "HDD", "500GB 3.5\"", "Desktop SATA hard disk", 2010, 20),
            ("Storage", "HDD SATA 1TB 3.5\"", null, "HDD", "1TB 3.5\"", "Desktop SATA hard disk", 2010, 30),
            ("Storage", "HDD SATA 500GB 2.5\"", null, "HDD", "500GB 2.5\"", "Laptop SATA hard disk", 2010, 40),
            ("Storage", "SSD SATA 120GB 2.5\"", null, "SSD", "120GB 2.5\"", "SATA SSD", 2012, 50),
            ("Storage", "SSD SATA 240GB 2.5\"", null, "SSD", "240GB 2.5\"", "SATA SSD", 2012, 60),
            ("Storage", "SSD SATA 480GB 2.5\"", null, "SSD", "480GB 2.5\"", "SATA SSD", 2013, 70),
            ("Storage", "SSD SATA 500GB 2.5\"", null, "SSD", "500GB 2.5\"", "SATA SSD", 2013, 80),
            ("Storage", "SSD SATA 1TB 2.5\"", null, "SSD", "1TB 2.5\"", "SATA SSD", 2013, 90),
            ("Storage", "NVMe M.2 256GB", null, "NVMe", "256GB M.2", "NVMe SSD", 2015, 100),
            ("Storage", "NVMe M.2 512GB", null, "NVMe", "512GB M.2", "NVMe SSD", 2015, 110),
            ("Storage", "NVMe M.2 1TB", null, "NVMe", "1TB M.2", "NVMe SSD", 2015, 120),

            ("StorageType", "HDD SATA", null, "HDD", "SATA", "Mechanical disk", 2010, 10),
            ("StorageType", "SSD SATA", null, "SSD", "SATA", "2.5 inch or M.2 SATA SSD", 2012, 20),
            ("StorageType", "NVMe M.2", null, "NVMe", "M.2", "NVMe SSD", 2015, 30),
            ("StorageType", "eMMC", null, "eMMC", "embedded", "Embedded flash storage", 2010, 40),

            ("Graphics", "Intel HD Graphics", "Intel", "HD Graphics", "Integrated", "Integrated graphics", 2010, 10),
            ("Graphics", "Intel UHD Graphics", "Intel", "UHD Graphics", "Integrated", "Integrated graphics", 2017, 20),
            ("Graphics", "AMD Radeon Vega Graphics", "AMD", "Radeon Vega", "Integrated", "Integrated graphics", 2018, 30),
            ("Graphics", "NVIDIA GeForce GT 710", "NVIDIA", "GeForce", "GT 710", "Entry-level GPU", 2014, 40),
            ("Graphics", "NVIDIA GeForce GT 1030", "NVIDIA", "GeForce", "GT 1030", "Entry-level GPU", 2017, 50),

            ("OperatingSystem", "Windows 10 Pro", "Microsoft", "Windows", "10 Pro", "Desktop OS", 2015, 10),
            ("OperatingSystem", "Windows 11 Pro", "Microsoft", "Windows", "11 Pro", "Desktop OS", 2021, 20),
            ("OperatingSystem", "Linux Mint", "Linux", "Mint", null, "Desktop Linux", 2010, 30),
            ("OperatingSystem", "Ubuntu", "Linux", "Ubuntu", null, "Desktop Linux", 2010, 40),
            ("OperatingSystem", "ChromeOS Flex", "Google", "ChromeOS Flex", null, "Lightweight OS", 2022, 50),

            ("PowerSupply", "ATX PSU 400W", null, "ATX", "400W", "Desktop power supply", 2010, 10),
            ("PowerSupply", "ATX PSU 500W", null, "ATX", "500W", "Desktop power supply", 2010, 20),
            ("PowerSupply", "SFX PSU 300W", null, "SFX", "300W", "Small form factor power supply", 2010, 30),
            ("PowerSupply", "Laptop charger 19V", null, "Laptop charger", "19V", "Generic laptop power adapter", 2010, 40)
        };

        foreach (var reference in references)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT OR IGNORE INTO TechnicalReferences
(ReferenceType, DisplayName, Manufacturer, Series, ModelName, Detail, ApproxYear, SortOrder, IsActive, IsBuiltIn, Notes, CreatedAt, UpdatedAt)
VALUES
(@type, @displayName, @manufacturer, @series, @modelName, @detail, @year, @sortOrder, 1, 1, NULL, @createdAt, @updatedAt);",
                new SqliteParameter("@type", reference.Type),
                new SqliteParameter("@displayName", reference.DisplayName),
                new SqliteParameter("@manufacturer", (object?)reference.Manufacturer ?? DBNull.Value),
                new SqliteParameter("@series", (object?)reference.Series ?? DBNull.Value),
                new SqliteParameter("@modelName", (object?)reference.ModelName ?? DBNull.Value),
                new SqliteParameter("@detail", (object?)reference.Detail ?? DBNull.Value),
                new SqliteParameter("@year", (object?)reference.Year ?? DBNull.Value),
                new SqliteParameter("@sortOrder", reference.SortOrder),
                new SqliteParameter("@createdAt", now),
                new SqliteParameter("@updatedAt", now));
        }
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
