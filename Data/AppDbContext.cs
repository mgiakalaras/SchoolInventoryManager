using Microsoft.EntityFrameworkCore;
using SchoolInventoryManager.Models;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolSettings>()
            .HasKey(x => x.Id);

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
    }
}
