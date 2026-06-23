using DomainDrivenDesign.Domain.Aggregates;
using DomainDrivenDesign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DomainDrivenDesign.Infrastructure.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
  public DbSet<Order> Orders => Set<Order>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Order>(entity =>
    {
      entity.OwnsOne(o => o.Total, money =>
      {
        money.Property(m => m.Amount).HasColumnName("TotalAmount");
        money.Property(m => m.Currency).HasColumnName("TotalCurrency");
      });

      entity.HasMany(o => o.OrderLines)
        .WithOne()
        .HasForeignKey(ol => ol.OrderId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasMany(o => o.StatusHistories)
        .WithOne()
        .HasForeignKey(sh => sh.OrderId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<OrderLine>(entity =>
    {
      entity.Ignore(ol => ol.LineTotal);

      entity.OwnsOne(ol => ol.ProductSnapshot, snapshot =>
      {
        snapshot.Property(ps => ps.ProductId).HasColumnName("ProductId");
        snapshot.Property(ps => ps.Name).HasColumnName("ProductName");
        snapshot.OwnsOne(ps => ps.Price, money =>
        {
          money.Property(m => m.Amount).HasColumnName("UnitPriceAmount");
          money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency");
        });
      });
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
