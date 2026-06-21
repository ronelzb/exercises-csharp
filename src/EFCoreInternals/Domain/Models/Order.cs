namespace EFCoreInternals.Domain.Models;

public class Order : EntityBase
{
  public virtual ICollection<OrderLine> OrderLines { get; set; } = [];
  public virtual ICollection<StatusHistory> StatusHistories { get; set; } = [];
}
