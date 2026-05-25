using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Models;
using SchoolInventoryManager.Utilities;

namespace SchoolInventoryManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SchoolSettings> SchoolSettings => Set<SchoolSettings>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryItemTechnicalSpecs> InventoryItemTechnicalSpecs => Set<InventoryItemTechnicalSpecs>();
    public DbSet<TechnicalReference> TechnicalReferences => Set<TechnicalReference>();
    public DbSet<SparePartStock> SparePartStocks => Set<SparePartStock>();
    public DbSet<SparePartUsageLog> SparePartUsageLogs => Set<SparePartUsageLog>();
    public DbSet<DestructionBatch> DestructionBatches => Set<DestructionBatch>();
    public DbSet<DestructionBatchItem> DestructionBatchItems => Set<DestructionBatchItem>();
    public DbSet<DestructionCommitteeMember> DestructionCommitteeMembers => Set<DestructionCommitteeMember>();
    public DbSet<InventoryAuditFolder> InventoryAuditFolders => Set<InventoryAuditFolder>();
    public DbSet<InventoryAuditRoomSession> InventoryAuditRoomSessions => Set<InventoryAuditRoomSession>();
    public DbSet<InventoryAuditScanLog> InventoryAuditScanLogs => Set<InventoryAuditScanLog>();

    public override int SaveChanges()
    {
        var newInventoryItems = GetNewInventoryItemsMissingQrIdentity();

        var result = base.SaveChanges();

        if (newInventoryItems.Count > 0)
        {
            EnsureAssetCodesAfterSave(newInventoryItems);
            result += base.SaveChanges();
        }

        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var newInventoryItems = GetNewInventoryItemsMissingQrIdentity();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (newInventoryItems.Count > 0)
        {
            EnsureAssetCodesAfterSave(newInventoryItems);
            result += await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private List<InventoryItem> GetNewInventoryItemsMissingQrIdentity()
    {
        return ChangeTracker
            .Entries<InventoryItem>()
            .Where(entry =>
                entry.State == EntityState.Added &&
                (string.IsNullOrWhiteSpace(entry.Entity.AssetCode) ||
                 string.IsNullOrWhiteSpace(entry.Entity.QrToken)))
            .Select(entry => entry.Entity)
            .ToList();
    }

    private void EnsureAssetCodesAfterSave(IEnumerable<InventoryItem> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.AssetCode))
            {
                item.AssetCode = AssetCodeGenerator.CreateAssetCode(item.Id, item.CreatedAt);
                Entry(item).Property(x => x.AssetCode).IsModified = true;
            }

            if (string.IsNullOrWhiteSpace(item.QrToken))
            {
                item.QrToken = AssetCodeGenerator.CreateQrToken();
                Entry(item).Property(x => x.QrToken).IsModified = true;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolSettings>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => x.AssetCode)
            .HasDatabaseName("IX_InventoryItems_AssetCode")
            .IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(x => x.QrToken)
            .HasDatabaseName("IX_InventoryItems_QrToken")
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Room)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryCategory>()
            .HasMany(x => x.Items)
            .WithOne(x => x.InventoryCategory)
            .HasForeignKey(x => x.InventoryCategoryId)
            .OnDelete(DeleteBehavior.SetNull);


        modelBuilder.Entity<TechnicalReference>()
            .HasIndex(x => new { x.ReferenceType, x.DisplayName })
            .IsUnique();

        modelBuilder.Entity<SparePartStock>()
            .HasIndex(x => new { x.PartType, x.IsActive });

        modelBuilder.Entity<SparePartStock>()
            .HasIndex(x => x.Name);

        modelBuilder.Entity<SparePartUsageLog>()
            .HasOne(x => x.SparePartStock)
            .WithMany(x => x.UsageLogs)
            .HasForeignKey(x => x.SparePartStockId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SparePartUsageLog>()
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SparePartUsageLog>()
            .HasIndex(x => new { x.SparePartStockId, x.UsedAt });

        modelBuilder.Entity<SparePartUsageLog>()
            .HasIndex(x => x.InventoryItemId);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(x => x.TechnicalSpecs)
            .WithOne(x => x.InventoryItem)
            .HasForeignKey<InventoryItemTechnicalSpecs>(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(x => x.DestructionBatch)
            .WithMany()
            .HasForeignKey(x => x.DestructionBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DestructionBatch>()
            .HasMany(x => x.Items)
            .WithOne(x => x.DestructionBatch)
            .HasForeignKey(x => x.DestructionBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DestructionBatch>()
            .HasMany(x => x.CommitteeMembers)
            .WithOne(x => x.DestructionBatch)
            .HasForeignKey(x => x.DestructionBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DestructionBatchItem>()
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryAuditFolder>()
            .HasMany(x => x.RoomSessions)
            .WithOne(x => x.InventoryAuditFolder)
            .HasForeignKey(x => x.InventoryAuditFolderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryAuditRoomSession>()
            .HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryAuditFolder>()
            .HasIndex(x => new { x.AuditDate, x.IsFinalized });

        modelBuilder.Entity<InventoryAuditRoomSession>()
            .HasIndex(x => new { x.InventoryAuditFolderId, x.RoomId });

        modelBuilder.Entity<InventoryAuditRoomSession>()
            .HasMany(x => x.ScanLogs)
            .WithOne(x => x.InventoryAuditRoomSession)
            .HasForeignKey(x => x.InventoryAuditRoomSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryAuditScanLog>()
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InventoryAuditScanLog>()
            .HasIndex(x => new { x.InventoryAuditRoomSessionId, x.Status })
            .HasDatabaseName("IX_InventoryAuditScanLogs_Session_Status");

        modelBuilder.Entity<InventoryAuditScanLog>()
            .HasIndex(x => new { x.InventoryAuditRoomSessionId, x.InventoryItemId })
            .HasDatabaseName("IX_InventoryAuditScanLogs_Session_Item");

        modelBuilder.Entity<InventoryAuditScanLog>()
            .HasIndex(x => new { x.InventoryAuditRoomSessionId, x.ScannedCode })
            .HasDatabaseName("IX_InventoryAuditScanLogs_Session_Code");
    }
}
