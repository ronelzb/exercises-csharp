namespace EFCoreInternals.Domain.Models;

public class OrderLine : EntityBase
{
  public Guid OrderId { get; set; }
  public virtual required Order Order { get; set; }

  public Guid ProductId { get; set; }
  public virtual required Product Product { get; set; }

  public int Quantity { get; set; }  // int simpler for arithmetic
  public decimal UnitPrice { get; set; }
}
