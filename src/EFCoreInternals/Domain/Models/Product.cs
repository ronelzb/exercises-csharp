namespace EFCoreInternals.Domain.Models;

public class Product : EntityBase
{
  public required string Name { get; set; }
  public decimal Price { get; set; }

  public virtual ICollection<OrderLine> OrderLines { get; set; } = [];
}
