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
