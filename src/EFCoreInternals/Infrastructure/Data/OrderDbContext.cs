using EFCoreInternals.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCoreInternals.Infrastructure.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderLine> OrderLines => Set<OrderLine>();
  public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Order>(entity =>
    {
      entity.HasMany(o => o.OrderLines)
        .WithOne(ol => ol.Order)
        .HasForeignKey(ol => ol.OrderId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasMany(o => o.StatusHistories)
        .WithOne(sh => sh.Order)
        .HasForeignKey(sh => sh.OrderId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<Product>(entity =>
    {
      entity.HasMany(p => p.OrderLines)
        .WithOne(ol => ol.Product)
        .HasForeignKey(ol => ol.ProductId)
        .OnDelete(DeleteBehavior.Restrict);
    });
  }

  private void UpdateTimestamps()
  {
    foreach (EntityEntry<EntityBase> entry in ChangeTracker.Entries<EntityBase>())
    {
      if (entry.State == EntityState.Modified)
        entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
    }
  }

  public override int SaveChanges()
  {
    UpdateTimestamps();
    return base.SaveChanges();
  }

  public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    UpdateTimestamps();
    return base.SaveChangesAsync(cancellationToken);
  }
}
